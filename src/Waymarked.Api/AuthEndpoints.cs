namespace Waymarked.Api;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Web;
using Waymarked.Api.Data;
using Waymarked.Api.Email;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest req, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IWaymarkedEmailSender emailSender, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuthEndpoints");
            if (string.IsNullOrEmpty(req.Email))
                return Results.BadRequest(new { errors = new[] { "Email is required." } });

            if (string.IsNullOrEmpty(req.Password))
                return Results.BadRequest(new { errors = new[] { "Password is required." } });

            var user = new ApplicationUser { Email = req.Email, UserName = req.Email };
            var result = await userManager.CreateAsync(user, req.Password);

            if (!result.Succeeded)
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description).ToArray() });

            await signInManager.SignInAsync(user, isPersistent: true);

            _ = emailSender.SendWelcomeEmailAsync(user, req.Email)
                .ContinueWith(t => logger.LogError(t.Exception, "Failed to send welcome email to {Email}", req.Email),
                    TaskContinuationOptions.OnlyOnFaulted);

            return Results.Ok();
        });

        group.MapPost("/login", async (LoginRequest req, SignInManager<ApplicationUser> signInManager) =>
        {
            if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
                return Results.BadRequest(new { errors = new[] { "Email and password are required." } });

            var result = await signInManager.PasswordSignInAsync(
                req.Email, req.Password, isPersistent: true, lockoutOnFailure: true);

            return result.Succeeded ? Results.Ok() : Results.Unauthorized();
        });

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.Ok();
        });

        group.MapGet("/me", (HttpContext context) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            return Results.Ok(new { email = context.User.Identity.Name });
        }).RequireAuthorization();

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest req,
            UserManager<ApplicationUser> userManager,
            IEmailSender<ApplicationUser> emailSender,
            IOptions<SmtpSettings> smtpOptions) =>
        {
            // Always return 200 to avoid revealing whether the email is registered.
            if (!string.IsNullOrEmpty(req.Email))
            {
                var user = await userManager.FindByEmailAsync(req.Email);
                if (user is not null)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var baseUrl = smtpOptions.Value.FrontendBaseUrl.TrimEnd('/');
                    var resetLink = $"{baseUrl}/?resetToken={HttpUtility.UrlEncode(token)}&email={HttpUtility.UrlEncode(req.Email)}";
                    await emailSender.SendPasswordResetLinkAsync(user, req.Email, resetLink);
                }
            }

            return Results.Ok();
        })
            .RequireRateLimiting("forgot-password");

        group.MapPost("/reset-password", async (
            ResetPasswordRequest req,
            UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Token) || string.IsNullOrEmpty(req.NewPassword))
                return Results.BadRequest(new { errors = new[] { "Email, token, and new password are required." } });

            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null)
                return Results.BadRequest(new { errors = new[] { "Password reset failed. The link may have expired." } });

            var result = await userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);

            if (!result.Succeeded)
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description).ToArray() });

            return Results.Ok();
        });
    }
}

record RegisterRequest(string Email, string Password);
record LoginRequest(string Email, string Password);
record ForgotPasswordRequest(string Email);
record ResetPasswordRequest(string Email, string Token, string NewPassword);
