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
    public async Task Post_ApiRoutes_AbRoute_Returns200WithDistanceKm()
    {
        var payload = new
        {
            from = new[] { 51.5074, -0.1278 },
            to   = new[] { 53.4808, -2.2426 },
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        // WaymarkedRouteResponse shape assertions
        doc.RootElement.TryGetProperty("distanceKm", out var distanceKm).Should().BeTrue();
        distanceKm.GetDouble().Should().BeGreaterThan(0);

        doc.RootElement.TryGetProperty("points", out var points).Should().BeTrue();
        points.TryGetProperty("coordinates", out var coords).Should().BeTrue();
        coords.GetArrayLength().Should().BeGreaterThan(0);

        doc.RootElement.TryGetProperty("isRoundTrip", out var isRoundTrip).Should().BeTrue();
        isRoundTrip.GetBoolean().Should().BeFalse("A to B route with a To point is not a round trip");
    }

    [Fact]
    public async Task Post_ApiRoutes_RoundTrip_Returns200WithDistanceKm()
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

        // WaymarkedRouteResponse shape assertions
        doc.RootElement.TryGetProperty("distanceKm", out var distanceKm).Should().BeTrue();
        distanceKm.GetDouble().Should().BeGreaterThan(0);

        doc.RootElement.TryGetProperty("points", out var points).Should().BeTrue();
        points.TryGetProperty("coordinates", out var coords).Should().BeTrue();
        coords.GetArrayLength().Should().BeGreaterThan(0);

        doc.RootElement.TryGetProperty("isRoundTrip", out var isRoundTrip).Should().BeTrue();
        isRoundTrip.GetBoolean().Should().BeTrue("no To provided means this is a round trip");
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

    [Fact]
    public async Task Post_ApiRoutes_FromOutsideGreatBritain_Returns400()
    {
        // Paris (lat=48.8566, lon=2.3522) is outside GB bounds (lat 49.8-60.9, lon -8.7-1.8)
        var payload = new
        {
            from = new[] { 48.8566, 2.3522 },
            to   = new[] { 51.5074, -0.1278 }, // London is valid, but From should fail first
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ApiRoutes_DistanceTooSmall_Returns400()
    {
        // 0.1 km = 100 m — below the 500 m minimum
        var payload = new
        {
            from = new[] { 51.5074, -0.1278 },
            distance = 0.1,
            distanceUnit = "kilometres",
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ApiRoutes_DistanceTooLarge_Returns400()
    {
        // 200 km = 200,000 m — above the 100 km maximum
        var payload = new
        {
            from = new[] { 51.5074, -0.1278 },
            distance = 200.0,
            distanceUnit = "kilometres",
            profile = "hike"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ExportGpx_Returns200WithGpxContentType()
    {
        var payload = new
        {
            from = new[] { 54.5994, -3.1367 },
            distance = 5.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes/export/gpx", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Contain("gpx");
    }

    [Fact]
    public async Task Post_ExportKml_Returns200WithKmlContentType()
    {
        var payload = new
        {
            from = new[] { 54.5994, -3.1367 },
            distance = 5.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes/export/kml", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Contain("kml");
    }

    [Fact]
    public async Task Post_ExportGeoJson_Returns200WithGeoJsonContentType()
    {
        var payload = new
        {
            from = new[] { 54.5994, -3.1367 },
            distance = 5.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes/export/geojson", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Contain("geo+json");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Export content-validation tests (expand the smoke tests above)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ExportGpx_ContainsValidGpxStructure()
    {
        var payload = new
        {
            from = new[] { 54.5994, -3.1367 },
            distance = 5.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes/export/gpx", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("<gpx",       "GPX files must start with a <gpx root element");
        body.Should().Contain("<trk>",      "GPX files must contain a track element");
        body.Should().Contain("<trkpt",     "GPX files must contain track-point elements");
        body.Should().Contain("lat=",       "track points must carry a lat attribute");
        body.Should().Contain("lon=",       "track points must carry a lon attribute");
    }

    [Fact]
    public async Task Post_ExportKml_ContainsValidKmlStructure()
    {
        var payload = new
        {
            from = new[] { 54.5994, -3.1367 },
            distance = 5.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes/export/kml", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("<kml",          "KML files must have a <kml root element");
        body.Should().Contain("<LineString>",  "KML route must be represented as a LineString");
        body.Should().Contain("<coordinates>", "KML LineString must contain a <coordinates> element");
    }

    [Fact]
    public async Task Post_ExportGeoJson_ContainsValidGeoJsonStructure()
    {
        var payload = new
        {
            from = new[] { 54.5994, -3.1367 },
            distance = 5.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes/export/geojson", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();

        // Accept both compact and indented JSON
        body.Should().MatchRegex("\"type\"\\s*:\\s*\"FeatureCollection\"",
            "top-level type must be FeatureCollection");
        body.Should().Contain("\"LineString\"",
            "geometry type must be LineString");
        body.Should().Contain("distanceKm",
            "properties must include distanceKm");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Sign + Interval in instructions
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ApiRoutes_ResponseIncludesSignAndIntervalInInstructions()
    {
        var payload = new
        {
            from = new[] { 54.5994, -3.1367 },
            distance = 5.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes", payload);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();

        // Deserialise using case-insensitive matching (API returns camelCase,
        // RouteInstruction uses [JsonPropertyName] attributes)
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var routeResponse = JsonSerializer.Deserialize<WaymarkedRouteResponse>(json, options);

        routeResponse.Should().NotBeNull();
        routeResponse!.Instructions.Should().NotBeNullOrEmpty("the stub returns one instruction");

        var first = routeResponse.Instructions![0];

        // Stub sets Sign = 2
        first.Sign.Should().Be(2, "the stub sets Sign = 2 on the first instruction");

        // Stub sets Interval = [0, 1]
        first.Interval.Should().NotBeNull("the stub sets Interval on the first instruction");
        first.Interval!.Should().HaveCount(2, "Interval must be a two-element [start, end] array");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Export endpoint validation — same rules apply as /api/routes
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ExportGpx_FromOutsideGreatBritain_Returns400()
    {
        // Paris coords (lat=48.8566, lon=2.3522) — outside GB bounds
        var payload = new
        {
            from = new[] { 48.8566, 2.3522 },
            distance = 5.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes/export/gpx", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "GPX export must reject coordinates outside Great Britain");
    }

    [Fact]
    public async Task Post_ExportGpx_DistanceTooLarge_Returns400()
    {
        // 200 km is above the 100 km maximum
        var payload = new
        {
            from = new[] { 54.5994, -3.1367 },
            distance = 200.0,
            distanceUnit = "kilometres",
            profile = "foot"
        };

        var response = await _client.PostAsJsonAsync("/api/routes/export/gpx", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "GPX export must reject distances exceeding 100 km");
    }
}
