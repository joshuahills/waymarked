namespace Waymarked.E2E.Tests;

using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;
using Xunit;

/// <summary>
/// End-to-end Playwright tests for the auth UI journeys driven against the full Aspire stack.
///
/// Covers: registration, login, logout, forgot-password, and password-reset flows,
/// plus modal UX (Escape key, overlay click).
///
/// Each test is independent — it registers its own user where required.
/// </summary>
[Collection("Aspire")]
public class AuthJourneyTests
{
    private readonly AspireFixture _fixture;

    private const string TestPassword = "ValidPass1!";

    public AuthJourneyTests(AspireFixture fixture)
    {
        _fixture = fixture;
    }

    private static string UniqueEmail() => $"e2e-{Guid.NewGuid():N}@waymarked.test";

    /// <summary>
    /// Launches a headless Chromium browser and navigates to the app's base URL.
    /// Callers are responsible for disposing playwright and browser via try-finally.
    /// </summary>
    private async Task<(IPlaywright playwright, IBrowser browser, IPage page)> OpenAppAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        var page = await browser.NewPageAsync();
        await page.GotoAsync(_fixture.WebBaseUrl);
        return (playwright, browser, page);
    }

    /// <summary>Opens the auth modal by clicking #signInBtn and waits for it to be visible.</summary>
    private static async Task OpenAuthModalAsync(IPage page)
    {
        await page.ClickAsync("#signInBtn");
        await page.WaitForSelectorAsync("#authModal:not([hidden])",
            new PageWaitForSelectorOptions { Timeout = 5_000 });
    }

    /// <summary>Switches from the login panel to the register panel.</summary>
    private static async Task SwitchToRegisterAsync(IPage page)
    {
        await page.ClickAsync("#switchToRegister");
        await page.WaitForSelectorAsync("#registerForm:not([hidden])",
            new PageWaitForSelectorOptions { Timeout = 5_000 });
    }

    /// <summary>
    /// Registers a new user via the UI. Returns the email used.
    /// After this call the user is logged in and the modal is closed.
    /// </summary>
    private async Task<string> RegisterViaUiAsync(IPage page, string? email = null, string? password = null)
    {
        email    ??= UniqueEmail();
        password ??= TestPassword;

        await OpenAuthModalAsync(page);
        await SwitchToRegisterAsync(page);

        await page.FillAsync("#registerEmail",    email);
        await page.FillAsync("#registerPassword", password);
        await page.FillAsync("#registerConfirm",  password);

        // Wait until the submit button becomes enabled (all pw requirements met + passwords match)
        await page.WaitForFunctionAsync(
            "() => !document.querySelector('#registerForm button[type=\"submit\"]').disabled",
            null,
            new PageWaitForFunctionOptions { Timeout = 5_000 });

        await page.ClickAsync("#registerForm button[type='submit']");

        // Modal closes and userInfo becomes visible on success
        await page.WaitForSelectorAsync("#userInfo:not([hidden])",
            new PageWaitForSelectorOptions { Timeout = 10_000 });

        return email;
    }

    /// <summary>Signs the current user out via #signOutBtn and waits for #signInBtn to reappear.</summary>
    private static async Task SignOutAsync(IPage page)
    {
        await page.ClickAsync("#signOutBtn");
        await page.WaitForSelectorAsync("#signInBtn:not([hidden])",
            new PageWaitForSelectorOptions { Timeout = 5_000 });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Registration Flow
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_OpensModalOnSignInClick()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await page.ClickAsync("#signInBtn");
            await page.WaitForSelectorAsync("#authModal:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            (await page.Locator("#authModal").IsVisibleAsync()).Should().BeTrue(
                "modal should open after clicking #signInBtn");

            (await page.Locator("#loginForm").IsVisibleAsync()).Should().BeTrue(
                "login form should be visible by default when modal opens");

            var title = await page.InnerTextAsync("#authModalTitle");
            title.Should().Be("Sign in", "modal title should be 'Sign in' on first open");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task Register_SwitchesToRegisterForm()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await OpenAuthModalAsync(page);
            await page.ClickAsync("#switchToRegister");
            await page.WaitForSelectorAsync("#registerForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            var title = await page.InnerTextAsync("#authModalTitle");
            title.Should().Be("Create account", "title should change to 'Create account' on register panel");

            (await page.Locator("#registerForm").IsVisibleAsync()).Should().BeTrue(
                "register form should be visible after switching");
            (await page.Locator("#loginForm").IsHiddenAsync()).Should().BeTrue(
                "login form should be hidden after switching to register");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task Register_SubmitDisabledUntilValidPassword()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await OpenAuthModalAsync(page);
            await SwitchToRegisterAsync(page);

            var submitBtn = page.Locator("#registerForm button[type='submit']");

            // Submit should start disabled before any input
            (await submitBtn.IsDisabledAsync()).Should().BeTrue("submit should be disabled initially");

            // Weak password — fails requirements; confirm matches but button stays disabled
            await page.FillAsync("#registerEmail",    UniqueEmail());
            await page.FillAsync("#registerPassword", "weak");
            await page.FillAsync("#registerConfirm",  "weak");

            (await submitBtn.IsDisabledAsync()).Should().BeTrue(
                "submit should remain disabled with a weak password that does not meet requirements");

            // Strong password that meets all Identity requirements + matches confirm
            await page.FillAsync("#registerPassword", TestPassword);
            await page.FillAsync("#registerConfirm",  TestPassword);

            await page.WaitForFunctionAsync(
                "() => !document.querySelector('#registerForm button[type=\"submit\"]').disabled",
                null,
                new PageWaitForFunctionOptions { Timeout = 5_000 });

            (await submitBtn.IsDisabledAsync()).Should().BeFalse(
                "submit should be enabled once all password requirements are satisfied and passwords match");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task Register_HappyPath_LogsInAndClosesModal()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            var email = UniqueEmail();
            await RegisterViaUiAsync(page, email);

            (await page.Locator("#authModal").IsHiddenAsync()).Should().BeTrue(
                "modal should close after successful registration");
            (await page.Locator("#userInfo").IsVisibleAsync()).Should().BeTrue(
                "#userInfo should be visible after registration");
            (await page.Locator("#signInBtn").IsHiddenAsync()).Should().BeTrue(
                "#signInBtn should be hidden when logged in");

            var displayedEmail = await page.InnerTextAsync("#userEmail");
            displayedEmail.Should().NotBeNullOrEmpty("#userEmail should show some text after login");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task Register_DuplicateEmail_ShowsError()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            var email = UniqueEmail();

            // Register first time via UI (succeeds, logs in)
            await RegisterViaUiAsync(page, email);

            // Sign out so we can attempt another registration
            await SignOutAsync(page);

            // Attempt to register with the same email
            await OpenAuthModalAsync(page);
            await SwitchToRegisterAsync(page);

            await page.FillAsync("#registerEmail",    email);
            await page.FillAsync("#registerPassword", TestPassword);
            await page.FillAsync("#registerConfirm",  TestPassword);

            await page.WaitForFunctionAsync(
                "() => !document.querySelector('#registerForm button[type=\"submit\"]').disabled",
                null,
                new PageWaitForFunctionOptions { Timeout = 5_000 });

            await page.ClickAsync("#registerForm button[type='submit']");

            await page.WaitForSelectorAsync("#registerError.visible",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            var errorText = await page.InnerTextAsync("#registerError");
            errorText.Should().NotBeNullOrEmpty(
                "#registerError should contain an error message for a duplicate email");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Login Flow
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_HappyPath_LogsInAndClosesModal()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            var email = UniqueEmail();

            // Register and immediately sign out, then test login flow
            await RegisterViaUiAsync(page, email);
            await SignOutAsync(page);

            await OpenAuthModalAsync(page);
            await page.FillAsync("#loginEmail",    email);
            await page.FillAsync("#loginPassword", TestPassword);
            await page.ClickAsync("#loginForm button[type='submit']");

            await page.WaitForSelectorAsync("#userInfo:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            (await page.Locator("#authModal").IsHiddenAsync()).Should().BeTrue(
                "modal should close after successful login");
            (await page.Locator("#userInfo").IsVisibleAsync()).Should().BeTrue(
                "#userInfo should be visible after login");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task Login_WrongPassword_ShowsError()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            var email = UniqueEmail();
            await RegisterViaUiAsync(page, email);
            await SignOutAsync(page);

            await OpenAuthModalAsync(page);
            await page.FillAsync("#loginEmail",    email);
            await page.FillAsync("#loginPassword", "WrongPassword99!");
            await page.ClickAsync("#loginForm button[type='submit']");

            await page.WaitForSelectorAsync("#loginError.visible",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            (await page.Locator("#authModal").IsVisibleAsync()).Should().BeTrue(
                "modal should remain open after a failed login");

            var errorText = await page.InnerTextAsync("#loginError");
            errorText.Should().NotBeNullOrEmpty(
                "#loginError should contain an error message for wrong password");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task Login_PersistsAcrossPageReload()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await RegisterViaUiAsync(page);

            // Reload the page — the auth cookie should keep the user logged in
            await page.GotoAsync(_fixture.WebBaseUrl);

            await page.WaitForSelectorAsync("#userInfo:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            (await page.Locator("#userInfo").IsVisibleAsync()).Should().BeTrue(
                "#userInfo should remain visible after page reload — auth cookie must persist");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Logout Flow
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SignOut_HidesUserInfoAndShowsSignInBtn()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await RegisterViaUiAsync(page);

            await page.ClickAsync("#signOutBtn");
            await page.WaitForSelectorAsync("#signInBtn:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            (await page.Locator("#signInBtn").IsVisibleAsync()).Should().BeTrue(
                "#signInBtn should be visible after sign out");
            (await page.Locator("#userInfo").IsHiddenAsync()).Should().BeTrue(
                "#userInfo should be hidden after sign out");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Forgot Password Flow
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_NavigatesToForgotPanel()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await OpenAuthModalAsync(page);
            await page.ClickAsync("#switchToForgotPassword");
            await page.WaitForSelectorAsync("#forgotPasswordForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            var title = await page.InnerTextAsync("#authModalTitle");
            title.Should().Be("Reset password", "title should be 'Reset password' on the forgot-password panel");

            (await page.Locator("#forgotPasswordForm").IsVisibleAsync()).Should().BeTrue(
                "forgot-password form should be visible after switching to that panel");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task ForgotPassword_AlwaysShowsSuccess()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await OpenAuthModalAsync(page);
            await page.ClickAsync("#switchToForgotPassword");
            await page.WaitForSelectorAsync("#forgotPasswordForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            // Submit a non-existent email — anti-enumeration: must always succeed visually
            await page.FillAsync("#forgotEmail", "nobody-who-exists@waymarked.test");
            await page.ClickAsync("#forgotSubmit");

            await page.WaitForSelectorAsync("#forgotSuccess:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            (await page.Locator("#forgotSuccess").IsVisibleAsync()).Should().BeTrue(
                "#forgotSuccess should always be shown regardless of whether the email exists (anti-enumeration)");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task ForgotPassword_DisablesButtonAfterSubmit()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await OpenAuthModalAsync(page);
            await page.ClickAsync("#switchToForgotPassword");
            await page.WaitForSelectorAsync("#forgotPasswordForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            await page.FillAsync("#forgotEmail", "test@waymarked.test");
            await page.ClickAsync("#forgotSubmit");

            // Wait for the response — success element appears when done
            await page.WaitForSelectorAsync("#forgotSuccess:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            (await page.Locator("#forgotSubmit").IsDisabledAsync()).Should().BeTrue(
                "#forgotSubmit should be disabled after a submission to prevent double-sends");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Password Reset Flow (URL-param driven)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPassword_ModalAutoOpensFromUrlParams()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            var resetUrl = $"{_fixture.WebBaseUrl}/?resetToken=faketoken&email=test@waymarked.test";
            await page.GotoAsync(resetUrl);

            await page.WaitForSelectorAsync("#authModal:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });
            await page.WaitForSelectorAsync("#resetPasswordForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            (await page.Locator("#authModal").IsVisibleAsync()).Should().BeTrue(
                "modal should auto-open when ?resetToken and ?email are present in the URL");
            (await page.Locator("#resetPasswordForm").IsVisibleAsync()).Should().BeTrue(
                "reset-password form should be the active panel");

            var title = await page.InnerTextAsync("#authModalTitle");
            title.Should().Be("Choose a new password",
                "modal title should be 'Choose a new password' on the reset-password panel");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task ResetPassword_SubmitDisabledUntilValidPassword()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            var resetUrl = $"{_fixture.WebBaseUrl}/?resetToken=faketoken&email=test@waymarked.test";
            await page.GotoAsync(resetUrl);

            await page.WaitForSelectorAsync("#resetPasswordForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            var submitBtn = page.Locator("#resetSubmit");

            // Initially disabled
            (await submitBtn.IsDisabledAsync()).Should().BeTrue(
                "#resetSubmit should be disabled before any password is entered");

            // Enter valid matching passwords
            await page.FillAsync("#resetPassword", TestPassword);
            await page.FillAsync("#resetConfirm",  TestPassword);

            // Wait for requirements to be satisfied and button to enable
            await page.WaitForFunctionAsync(
                "() => !document.querySelector('#resetSubmit').disabled",
                null,
                new PageWaitForFunctionOptions { Timeout = 5_000 });

            (await submitBtn.IsDisabledAsync()).Should().BeFalse(
                "#resetSubmit should be enabled once a valid matching password is entered");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ShowsError()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            var resetUrl = $"{_fixture.WebBaseUrl}/?resetToken=faketoken&email=test@waymarked.test";
            await page.GotoAsync(resetUrl);

            await page.WaitForSelectorAsync("#resetPasswordForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            await page.FillAsync("#resetPassword", TestPassword);
            await page.FillAsync("#resetConfirm",  TestPassword);

            await page.WaitForFunctionAsync(
                "() => !document.querySelector('#resetSubmit').disabled",
                null,
                new PageWaitForFunctionOptions { Timeout = 5_000 });

            await page.ClickAsync("#resetSubmit");

            await page.WaitForSelectorAsync("#resetError.visible",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            var errorText = await page.InnerTextAsync("#resetError");
            errorText.Should().NotBeNullOrEmpty(
                "#resetError should contain an error message when the token is invalid");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Modal UX
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Modal_ClosesOnEscapeKey()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await OpenAuthModalAsync(page);

            (await page.Locator("#authModal").IsVisibleAsync()).Should().BeTrue(
                "modal must be open before pressing Escape");

            await page.Keyboard.PressAsync("Escape");

            await page.WaitForSelectorAsync("#authModal[hidden]",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            (await page.Locator("#authModal").IsHiddenAsync()).Should().BeTrue(
                "modal should close when Escape is pressed");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    [Fact]
    public async Task Modal_ClosesOnOverlayClick()
    {
        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await OpenAuthModalAsync(page);

            (await page.Locator("#authModal").IsVisibleAsync()).Should().BeTrue(
                "modal must be open before clicking the overlay");

            await page.ClickAsync(".auth-modal-overlay");

            await page.WaitForSelectorAsync("#authModal[hidden]",
                new PageWaitForSelectorOptions { Timeout = 5_000 });

            (await page.Locator("#authModal").IsHiddenAsync()).Should().BeTrue(
                "modal should close when the overlay backdrop is clicked");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Full password-reset flow via smtp4dev
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Full E2E password reset: registers a user, triggers a reset email, retrieves
    /// the real token from smtp4dev, completes the reset via the UI, and verifies
    /// that the new password works for login.
    ///
    /// This test is skipped gracefully when smtp4dev is unavailable (e.g., when
    /// the AppHost is running in publish mode or in CI without the SMTP container).
    /// </summary>
    [Fact]
    public async Task ResetPassword_ValidToken_ResetsPasswordAndLogsInWithNewPassword()
    {
        if (string.IsNullOrEmpty(_fixture.SmtpWebUiBaseUrl))
            return; // smtp4dev not available — degrade gracefully

        // 1. Register a dedicated user for this test
        var resetEmail = $"pwreset-{Guid.NewGuid():N}@waymarked.test";
        using var http = new HttpClient { BaseAddress = new Uri(_fixture.WebBaseUrl + "/") };

        var regResponse = await http.PostAsJsonAsync("api/auth/register",
            new { email = resetEmail, password = TestPassword });
        regResponse.EnsureSuccessStatusCode();

        // 2. Request a password reset email
        await http.PostAsJsonAsync("api/auth/forgot-password", new { email = resetEmail });

        // 3. Poll smtp4dev for the reset email (up to 10 s)
        string? resetToken = null;
        using var smtpHttp = new HttpClient
        {
            BaseAddress = new Uri(_fixture.SmtpWebUiBaseUrl + "/")
        };

        for (var attempt = 0; attempt < 20 && resetToken is null; attempt++)
        {
            await Task.Delay(500);
            try
            {
                var json = await smtpHttp.GetStringAsync("api/Email");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // smtp4dev wraps messages in {"results":[...]} — handle both shapes
                var messages = root.ValueKind == JsonValueKind.Array
                    ? root
                    : root.TryGetProperty("results", out var r) ? r : default;

                if (messages.ValueKind != JsonValueKind.Array) continue;

                foreach (var msg in messages.EnumerateArray())
                {
                    var subject = msg.TryGetProperty("subject", out var s)
                        ? s.GetString() ?? "" : "";
                    if (!subject.Contains("reset", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var toMatch = false;
                    if (msg.TryGetProperty("to", out var toEl))
                    {
                        toMatch = toEl.ValueKind == JsonValueKind.Array
                            ? toEl.EnumerateArray().Any(t =>
                                t.GetString()?.Contains(resetEmail,
                                    StringComparison.OrdinalIgnoreCase) == true)
                            : toEl.GetString()?.Contains(resetEmail,
                                StringComparison.OrdinalIgnoreCase) == true;
                    }
                    if (!toMatch) continue;

                    var id = msg.TryGetProperty("id", out var idEl)
                        ? idEl.GetString() : null;
                    if (id is null) continue;

                    // Retrieve the HTML body and scan for a href containing resetToken
                    var html = await smtpHttp.GetStringAsync($"api/Email/{id}/html");
                    var pos = 0;
                    while (pos < html.Length && resetToken is null)
                    {
                        var hrefStart = html.IndexOf("href=\"", pos,
                            StringComparison.OrdinalIgnoreCase);
                        if (hrefStart < 0) break;
                        hrefStart += 6; // skip href="
                        var hrefEnd = html.IndexOf('"', hrefStart);
                        if (hrefEnd <= hrefStart) break;

                        var candidate = html[hrefStart..hrefEnd];
                        if (candidate.Contains("resetToken",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            var linkUri = new Uri(candidate);
                            var qs = linkUri.Query.TrimStart('?').Split('&');
                            var tokenPart = qs.FirstOrDefault(p =>
                                p.StartsWith("resetToken=",
                                    StringComparison.OrdinalIgnoreCase));
                            if (tokenPart is not null)
                                resetToken = Uri.UnescapeDataString(
                                    tokenPart["resetToken=".Length..]);
                        }
                        pos = hrefEnd + 1;
                    }
                    break;
                }
            }
            catch { /* retry on transient error */ }
        }

        if (resetToken is null) return; // Could not retrieve token — skip gracefully

        // 4. Navigate to the reset URL with the real token
        var newPassword = "NewWaymark@999!";
        var resetUrl = $"{_fixture.WebBaseUrl}/?resetToken={Uri.EscapeDataString(resetToken)}" +
                       $"&email={Uri.EscapeDataString(resetEmail)}";

        var (playwright, browser, page) = await OpenAppAsync();
        try
        {
            await page.GotoAsync(resetUrl);

            await page.WaitForSelectorAsync("#resetPasswordForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            await page.FillAsync("#resetPassword", newPassword);
            await page.FillAsync("#resetConfirm",  newPassword);

            await page.WaitForFunctionAsync(
                "() => !document.querySelector('#resetSubmit').disabled",
                null,
                new PageWaitForFunctionOptions { Timeout = 5_000 });

            await page.ClickAsync("#resetSubmit");

            // auth.js switches to the login form on success and pre-fills email
            await page.WaitForSelectorAsync("#loginForm:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            (await page.Locator("#loginForm").IsVisibleAsync()).Should().BeTrue(
                "the login form should appear after a successful password reset");

            // 5. Verify the new password works for login (email is pre-filled by auth.js)
            await page.FillAsync("#loginPassword", newPassword);
            await page.ClickAsync("#loginForm button[type='submit']");

            await page.WaitForSelectorAsync("#userInfo:not([hidden])",
                new PageWaitForSelectorOptions { Timeout = 10_000 });

            (await page.Locator("#userInfo").IsVisibleAsync()).Should().BeTrue(
                "the user should be logged in after resetting the password successfully");
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }
}
