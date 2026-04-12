using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Waymarked.Routing;

namespace Waymarked.Routing.Tests;

/// <summary>
/// Tests GraphHopperClient query string construction by intercepting HttpClient requests
/// via a custom DelegatingHandler. We capture the request URI and return a canned response.
/// </summary>
public class GraphHopperClientTests
{
    private static (GraphHopperClient client, List<Uri> capturedUris) CreateClientWithCapture(
        RouteResponse? responsePayload = null)
    {
        var capturedUris = new List<Uri>();
        var payload = responsePayload ?? new RouteResponse
        {
            Paths =
            [
                new RoutePath { Distance = 5000, Time = 3600000 }
            ]
        };
        var json = JsonSerializer.Serialize(payload);

        var handler = new CapturingHandler(capturedUris, json);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://graphhopper")
        };

        var logger = NullLogger<GraphHopperClient>.Instance;
        var client = new GraphHopperClient(httpClient, logger);
        return (client, capturedUris);
    }

    [Fact]
    public async Task GetRouteAsync_AbRouting_BuildsCorrectQueryString()
    {
        var (client, capturedUris) = CreateClientWithCapture();

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            To = [53.4808, -2.2426],
            Profile = "hike"
        };

        await client.GetRouteAsync(request);

        capturedUris.Should().HaveCount(1);
        var uri = capturedUris[0].ToString();

        uri.Should().Contain("point=51.5074,-0.1278");
        uri.Should().Contain("point=53.4808,-2.2426");
        uri.Should().Contain("profile=hike");
        uri.Should().Contain("locale=en-GB");
        uri.Should().Contain("instructions=true");
        uri.Should().Contain("calc_points=true");
        uri.Should().Contain("elevation=true");
        uri.Should().Contain("points_encoded=false");
        uri.Should().NotContain("algorithm=round_trip");
    }

    [Fact]
    public async Task GetRouteAsync_RoundTrip_UsesRoundTripAlgorithm()
    {
        var (client, capturedUris) = CreateClientWithCapture();

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10000 // 10km in metres
        };

        await client.GetRouteAsync(request);

        capturedUris.Should().HaveCount(1);
        var uri = capturedUris[0].ToString();

        uri.Should().Contain("point=51.5074,-0.1278");
        uri.Should().Contain("algorithm=round_trip");
        uri.Should().Contain("round_trip.distance=10000");
        uri.Should().Contain("round_trip.seed=");
        // Should NOT have a second point parameter for destination
        uri.Should().NotContain("algorithm=round_trip&point="); // no destination point after round_trip
    }

    [Fact]
    public async Task GetRouteAsync_RoundTrip_IncludesSinglePointOnly()
    {
        var (client, capturedUris) = CreateClientWithCapture();

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 5000
        };

        await client.GetRouteAsync(request);

        var uri = capturedUris[0].ToString();
        // Count occurrences of "point=" — should be exactly 1 (only From)
        var pointCount = System.Text.RegularExpressions.Regex.Matches(uri, @"point=").Count;
        pointCount.Should().Be(1, "round-trip routing uses a single start point");
    }

    [Fact]
    public async Task GetRouteAsync_AbRouting_IncludesTwoPoints()
    {
        var (client, capturedUris) = CreateClientWithCapture();

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            To = [53.4808, -2.2426]
        };

        await client.GetRouteAsync(request);

        var uri = capturedUris[0].ToString();
        var pointCount = System.Text.RegularExpressions.Regex.Matches(uri, @"point=").Count;
        pointCount.Should().Be(2, "A→B routing uses two point parameters");
    }

    [Fact]
    public async Task GetRouteAsync_ReturnsDeserializedResponse()
    {
        var expectedResponse = new RouteResponse
        {
            Paths = [new RoutePath { Distance = 12345.6, Time = 7200000 }]
        };
        var (client, _) = CreateClientWithCapture(expectedResponse);

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            To = [53.4808, -2.2426]
        };

        var result = await client.GetRouteAsync(request);

        result.Should().NotBeNull();
        result!.Paths.Should().HaveCount(1);
        result.Paths[0].Distance.Should().BeApproximately(12345.6, 0.1);
    }

    [Fact]
    public async Task GetRouteAsync_HttpFailure_ThrowsHttpRequestException()
    {
        var capturedUris = new List<Uri>();
        var handler = new CapturingHandler(capturedUris, "", HttpStatusCode.ServiceUnavailable);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://graphhopper") };
        var client = new GraphHopperClient(httpClient, NullLogger<GraphHopperClient>.Instance);

        var request = new RouteRequest { From = [51.5074, -0.1278], To = [53.4808, -2.2426] };

        await client.Invoking(c => c.GetRouteAsync(request))
            .Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetRouteAsync_CustomProfile_IncludedInQueryString()
    {
        var (client, capturedUris) = CreateClientWithCapture();

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            To = [53.4808, -2.2426],
            Profile = "mtb"
        };

        await client.GetRouteAsync(request);

        capturedUris[0].ToString().Should().Contain("profile=mtb");
    }

    [Fact]
    public async Task GetRouteAsync_RoundTrip_DistanceConvertedToInt()
    {
        var (client, capturedUris) = CreateClientWithCapture();

        // 10.7km expressed as double metres
        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10700.9 // will be truncated to int by Convert.ToInt32
        };

        await client.GetRouteAsync(request);

        var uri = capturedUris[0].ToString();
        uri.Should().Contain("round_trip.distance=10701"); // Convert.ToInt32 rounds, not truncates
    }
}

/// <summary>
/// Custom DelegatingHandler that captures request URIs and returns a canned JSON response.
/// </summary>
internal class CapturingHandler(
    List<Uri> capturedUris,
    string responseJson,
    HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        capturedUris.Add(request.RequestUri!);
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(response);
    }
}
