namespace Waymarked.Api.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Waymarked.Api.Data;
using Waymarked.Api.Email;
using Waymarked.Routing;

/// <summary>
/// WebApplicationFactory for auth endpoint tests.
/// Stubs GraphHopper and replaces the Npgsql DbContext with an InMemory database
/// so auth tests are fully self-contained — no PostgreSQL, no GraphHopper needed.
/// Each factory instance gets its own isolated InMemory database.
/// </summary>
public class AuthWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"WaymarkedAuthTest_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // ── GraphHopper stub ──────────────────────────────────────────────
            var toRemove = services
                .Where(d => d.ServiceType.FullName?.Contains("GraphHopper") == true ||
                            d.ImplementationType?.FullName?.Contains("GraphHopper") == true)
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            services.AddHttpClient<GraphHopperClient>(client =>
                client.BaseAddress = new Uri("http://fake-graphhopper"))
                .AddHttpMessageHandler(() => new StubGraphHopperHandler());

            // ── InMemory database (replaces Npgsql) ───────────────────────────
            CustomWebApplicationFactory.ReplaceDbContextWithInMemory(services, _dbName);

            // ── Stub email sender (no actual SMTP in tests) ──────────────────
            services.RemoveAll<IEmailSender<ApplicationUser>>();
            services.RemoveAll<IWaymarkedEmailSender>();
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender<ApplicationUser>>(sp => sp.GetRequiredService<FakeEmailSender>());
            services.AddSingleton<IWaymarkedEmailSender>(sp => sp.GetRequiredService<FakeEmailSender>());

            // ── SMTP settings with valid FrontendBaseUrl ──────────────────────
            services.Configure<SmtpSettings>(options =>
            {
                options.FrontendBaseUrl = "https://test.waymarked.local";
            });

            // ── Disable lockout in tests to avoid cross-test interference ────
            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.AllowedForNewUsers = false;
                options.Lockout.MaxFailedAccessAttempts = int.MaxValue;
            });

            // Test clients use http://localhost — secure-only cookies won't be sent.
            // Override to None so the CookieContainer propagates the auth cookie between requests.
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
        });

        builder.UseEnvironment("Test");
    }
}

/// <summary>
/// Fake email sender for tests — logs email sends but doesn't actually send anything.
/// </summary>
internal class FakeEmailSender : IEmailSender<ApplicationUser>, IWaymarkedEmailSender
{
    public Task SendWelcomeEmailAsync(ApplicationUser user, string email) =>
        Task.CompletedTask;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        Task.CompletedTask;
}
