using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Waymarked.Routing;

namespace Waymarked.Api.Tests;

/// <summary>
/// Custom WebApplicationFactory that replaces the real GraphHopper HttpClient
/// with a stub that returns a canned RouteResponse — no live GraphHopper needed.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real HttpClient registration for GraphHopperClient
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IHttpClientFactory));
            
            // Replace the named/typed HttpClient for GraphHopperClient with a stub
            // We do this by removing existing HttpClient factories and adding our own
            var toRemove = services
                .Where(d => d.ServiceType.FullName?.Contains("GraphHopper") == true ||
                            (d.ImplementationType?.FullName?.Contains("GraphHopper") == true))
                .ToList();
            
            foreach (var d in toRemove)
                services.Remove(d);

            // Register a stub GraphHopperClient using a fake HttpMessageHandler
            services.AddHttpClient<GraphHopperClient>(client =>
            {
                client.BaseAddress = new Uri("http://fake-graphhopper");
            })
            .AddHttpMessageHandler(() => new StubGraphHopperHandler());
        });

        builder.UseEnvironment("Test");
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
                        StreetName = "High Street"
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
