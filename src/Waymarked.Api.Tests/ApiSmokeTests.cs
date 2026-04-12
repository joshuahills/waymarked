using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Waymarked.Routing;

namespace Waymarked.Api.Tests;

/// <summary>
/// Integration smoke tests for the Waymarked API using WebApplicationFactory.
/// GraphHopperClient's underlying HttpClient is stubbed out — no real GraphHopper needed.
/// </summary>
public class ApiSmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_Root_Returns200WithMessage()
    {
        var response = await _client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Waymarked");
    }

    [Fact]
    public async Task Post_ApiRoutes_AbRoute_Returns200WithPaths()
    {
        var payload = new
        {
            from = new[] { 51.5074, -0.1278 },
            to = new[] { 53.4808, -2.2426 },
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("paths", out var paths).Should().BeTrue();
        paths.GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Post_ApiRoutes_RoundTrip_Returns200WithPaths()
    {
        var payload = new
        {
            from = new[] { 51.5074, -0.1278 },
            distance = 10.0,
            distanceUnit = "kilometres",
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("paths", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Post_ApiRoutes_MissingFrom_Returns400ValidationProblem()
    {
        var payload = new
        {
            to = new[] { 53.4808, -2.2426 },
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ApiRoutes_RoundTrip_MissingDistance_Returns400()
    {
        var payload = new
        {
            from = new[] { 51.5074, -0.1278 },
            // No To, no Distance — should fail validation
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ApiRoutes_MilesDistanceUnit_IsAccepted()
    {
        var payload = new
        {
            from = new[] { 51.5074, -0.1278 },
            distance = 6.2,
            distanceUnit = "miles",
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
