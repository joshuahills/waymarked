namespace Waymarked.Api;

using Microsoft.AspNetCore.Identity;
using Waymarked.Api.Data;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest req, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
        {
            if (string.IsNullOrEmpty(req.Email))
                return Results.BadRequest(new { errors = new[] { "Email is required." } });

            if (string.IsNullOrEmpty(req.Password))
                return Results.BadRequest(new { errors = new[] { "Password is required." } });

            var user = new ApplicationUser { Email = req.Email, UserName = req.Email };
            var result = await userManager.CreateAsync(user, req.Password);

            if (!result.Succeeded)
                return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description).ToArray() });

            await signInManager.SignInAsync(user, isPersistent: true);

            return Results.Ok();
        });

        group.MapPost("/login", async (LoginRequest req, SignInManager<ApplicationUser> signInManager) =>
        {
            if (string.IsNullOrEmpty(req.Email) || string.IsNullOrEmpty(req.Password))
                return Results.BadRequest(new { errors = new[] { "Email and password are required." } });

            var result = await signInManager.PasswordSignInAsync(
                req.Email, req.Password, isPersistent: true, lockoutOnFailure: false);

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
    }
}

record RegisterRequest(string Email, string Password);
record LoginRequest(string Email, string Password);
