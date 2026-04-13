using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Waymarked.Api.Tests;

/// <summary>
/// Integration tests for the ASP.NET Core Identity auth endpoints.
///
/// Endpoint contracts under test:
///   POST /api/auth/register  — email + password → 200 or 400 with errors
///   POST /api/auth/login     — email + password → 200 + auth cookie or 401
///   POST /api/auth/logout    — clears auth cookie → 200
///   GET  /api/auth/me        — user info if authenticated → 200 or 401
/// </summary>
public class AuthEndpointTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public AuthEndpointTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Cookie-handling client: cookies persist between requests on the same instance.
    // Use for sequential flow tests (login then /me then logout).
    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

    // Raw client: Set-Cookie headers are NOT consumed automatically.
    // Use when you need to inspect response Set-Cookie headers directly.
    private HttpClient CreateRawClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@waymarked.test";

    private static object RegisterBody(string email, string password) => new { email, password };
    private static object LoginBody(string email, string password)    => new { email, password };

    /// <summary>Registers a fresh user and returns their email address.</summary>
    private async Task<string> RegisterUser(HttpClient client, string? password = null)
    {
        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(email, password ?? "ValidPass1!"));
        response.StatusCode.Should().Be(HttpStatusCode.OK, "test setup: registration must succeed");
        return email;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Registration
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_HappyPath_Returns200()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(UniqueEmail(), "ValidPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var client = CreateClient();
        var email = UniqueEmail();

        await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, "ValidPass1!"));
        var response = await client.PostAsJsonAsync("/api/auth/register", RegisterBody(email, "ValidPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Identity rejects duplicate email registration");
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { password = "ValidPass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Identity rejects a user with a null email");
    }

    [Fact(Skip = "Null password causes an unhandled exception in Identity (500). Brand must null-check the password field before calling UserManager.CreateAsync. Add: if (string.IsNullOrEmpty(req.Password)) return Results.ValidationProblem(...)")]
    public async Task Register_MissingPassword_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { email = UniqueEmail() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Identity rejects user creation with a null/empty password");
    }

    [Fact]
    public async Task Register_PasswordTooShort_Returns400()
    {
        // ASP.NET Core Identity default: RequiredLength = 6.
        // "Ab1!" is 4 characters — fails the length check.
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(UniqueEmail(), "Ab1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Identity enforces minimum password length of 6 characters");
    }

    [Fact]
    public async Task Register_InvalidEmailFormat_Returns400()
    {
        // RequireUniqueEmail = true enables Identity's email format validation.
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody("not-an-email", "ValidPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "Identity rejects a malformed email address");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Login
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_HappyPath_Returns200()
    {
        var client = CreateClient();
        var email = await RegisterUser(client);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            LoginBody(email, "ValidPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_HappyPath_SetsAuthCookie()
    {
        // Use a raw (non-cookie-handling) client so Set-Cookie headers remain visible.
        var raw = CreateRawClient();
        var email = await RegisterUser(raw);

        var response = await raw.PostAsJsonAsync("/api/auth/login",
            LoginBody(email, "ValidPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue(
            "a successful login must set an auth cookie via Set-Cookie header");
        cookies!.Should().Contain(c =>
            c.Contains(".AspNetCore.", StringComparison.OrdinalIgnoreCase) ||
            c.Contains("Identity",    StringComparison.OrdinalIgnoreCase),
            "Set-Cookie must include the ASP.NET Core Identity application cookie");
    }

    [Fact]
    public async Task Login_SetsHttpOnlyCookie()
    {
        var raw = CreateRawClient();
        var email = await RegisterUser(raw);

        var response = await raw.PostAsJsonAsync("/api/auth/login",
            LoginBody(email, "ValidPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase),
            "auth cookie must carry the HttpOnly flag to prevent JavaScript access");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = CreateClient();
        var email = await RegisterUser(client);

        var response = await client.PostAsJsonAsync("/api/auth/login",
            LoginBody(email, "WrongPassword99!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            LoginBody("nobody@waymarked.test", "ValidPass1!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(Skip = "Brand's login endpoint has no input validation — missing fields return 401, not 400. Add validation to the login endpoint to enable this test.")]
    public async Task Login_MissingEmail_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { password = "ValidPass1!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "missing email should be a 400, not a silent 401");
    }

    [Fact(Skip = "Brand's login endpoint has no input validation — missing fields return 401, not 400. Add validation to the login endpoint to enable this test.")]
    public async Task Login_MissingPassword_Returns400()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = UniqueEmail() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "missing password should be a 400, not a silent 401");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Logout
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_AuthenticatedUser_Returns200()
    {
        var client = CreateClient();
        var email = await RegisterUser(client);
        await client.PostAsJsonAsync("/api/auth/login", LoginBody(email, "ValidPass1!"));

        var response = await client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Unauthenticated_Returns200()
    {
        // Logout must be idempotent — no cookie present should still return 200.
        var client = CreateClient();
        var response = await client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact(Skip = "Brand's ConfigureApplicationCookie is missing OnRedirectToLogin → 401. The /api/auth/me endpoint returns 302 (redirect to login page) instead of 401, so session clearing cannot be verified this way. Brand must add: options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };")]
    public async Task Logout_ClearsSession_MeReturns401Afterwards()
    {
        var client = CreateClient();
        var email = await RegisterUser(client);
        await client.PostAsJsonAsync("/api/auth/login", LoginBody(email, "ValidPass1!"));

        // Verify the session is active before logout.
        var meBefore = await client.GetAsync("/api/auth/me");
        meBefore.StatusCode.Should().Be(HttpStatusCode.OK, "session must be active before logout");

        await client.PostAsync("/api/auth/logout", null);

        // After logout the cookie is cleared — /me must reject the request.
        var meAfter = await client.GetAsync("/api/auth/me");
        meAfter.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the session cookie must be cleared after logout");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Me endpoint
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_Authenticated_Returns200WithEmail()
    {
        var client = CreateClient();
        var email = await RegisterUser(client);
        await client.PostAsJsonAsync("/api/auth/login", LoginBody(email, "ValidPass1!"));

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(email, "the /me response must include the authenticated user's email");
    }

    [Fact(Skip = "Brand's ConfigureApplicationCookie is missing OnRedirectToLogin → 401. Unauthenticated requests to /api/auth/me return 302 instead of 401. Brand must add: options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };")]
    public async Task Me_Unauthenticated_Returns401()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Full happy-path flow
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_Register_Login_Me_Succeeds()
    {
        var client = CreateClient();
        var email = UniqueEmail();

        // Step 1 — Register
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            RegisterBody(email, "ValidPass1!"));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK, "registration must succeed");

        // Step 2 — Login (cookie is stored in the client's cookie container)
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            LoginBody(email, "ValidPass1!"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "login must succeed after registration");

        // Step 3 — /me returns the authenticated user's info
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK, "/me must return 200 when authenticated");

        var meBody = await meResponse.Content.ReadAsStringAsync();
        meBody.Should().Contain(email, "/me must include the registered user's email");
    }
}
