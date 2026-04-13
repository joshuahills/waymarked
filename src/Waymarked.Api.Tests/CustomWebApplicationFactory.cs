using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Waymarked.Api.Data;
using Waymarked.Routing;

namespace Waymarked.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory that replaces the real GraphHopper HttpClient
/// with a stub that returns a canned RouteResponse — no live GraphHopper needed.
/// Also substitutes an EF Core InMemory database so startup's EnsureCreatedAsync
/// succeeds without a real PostgreSQL instance.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"WaymarkedSmokeTest_{Guid.NewGuid():N}";

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
            // Aspire's AddNpgsqlDbContext registers IConfigureOptions<DbContextOptions<T>>
            // that validates the Postgres connection string at options-build time.
            // We must remove ALL WaymarkedDbContext-related registrations and replace
            // them with an InMemory provider so EnsureCreatedAsync succeeds in tests.
            ReplaceDbContextWithInMemory(services, _dbName);
        });

        builder.UseEnvironment("Test");
    }

    internal static void ReplaceDbContextWithInMemory(IServiceCollection services, string dbName)
    {
        // Remove ALL registrations that reference WaymarkedDbContext, including:
        //  - WaymarkedDbContext (scoped context registration)
        //  - DbContextOptions<WaymarkedDbContext> (options descriptor)
        //  - IConfigureOptions<DbContextOptions<WaymarkedDbContext>> (Aspire's validation callback)
        //  - Any other generic types parameterised on WaymarkedDbContext
        var dbContextType = typeof(WaymarkedDbContext);
        var configureOptionsType =
            typeof(Microsoft.Extensions.Options.IConfigureOptions<DbContextOptions<WaymarkedDbContext>>);

        var toRemove = services
            .Where(d =>
                d.ServiceType == dbContextType ||
                d.ServiceType == typeof(DbContextOptions<WaymarkedDbContext>) ||
                d.ServiceType == configureOptionsType ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GenericTypeArguments.Contains(dbContextType)))
            .ToList();

        foreach (var d in toRemove)
            services.Remove(d);

        services.AddDbContext<WaymarkedDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
    }
}

/// <summary>
/// Returns a canned RouteResponse JSON for any request — simulates GraphHopper.
/// </summary>
internal class StubGraphHopperHandler : DelegatingHandler
{
    private static readonly RouteResponse CannedResponse = new()
    {
        Paths =
        [
            new RoutePath
            {
                Distance = 5432.1,
                Time = 3600000,
                Ascend = 120.5,
                Descend = 95.2,
                Points = new RoutePoints
                {
                    Type = "LineString",
                    Coordinates = [[51.5074, -0.1278], [52.0, -0.5], [53.4808, -2.2426]]
                },
                Instructions =
                [
                    new RouteInstruction
                    {
                        Distance = 100,
                        Time = 72000,
                        Text = "Head north",
                        StreetName = "High Street",
                        Sign = 2,
                        Interval = [0, 1]
                    }
                ]
            }
        ]
    };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(CannedResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}
