namespace Waymarked.Api.Tests;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
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
        });

        builder.UseEnvironment("Test");
    }
}
