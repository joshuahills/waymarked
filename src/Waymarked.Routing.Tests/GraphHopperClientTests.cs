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

    [Fact]
    public async Task GetRouteAsync_RoundTrip_IncludesMaxRetriesParam()
    {
        var (client, capturedUris) = CreateClientWithCapture();

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10000
        };

        await client.GetRouteAsync(request);

        capturedUris[0].ToString().Should().Contain("round_trip.max_retries=10");
    }

    [Fact]
    public async Task GetRouteAsync_RoundTrip_RetriesUpToMaxWhenDeviationExceedsTolerance()
    {
        var capturedUris = new List<Uri>();
        // Stub returns 5000m for a 10000m request — 50% deviation, well above 15% tolerance
        var response = new RouteResponse { Paths = [new RoutePath { Distance = 5000 }] };
        var handler = new CapturingHandler(capturedUris, JsonSerializer.Serialize(response));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://graphhopper") };
        var client = new GraphHopperClient(httpClient, NullLogger<GraphHopperClient>.Instance);

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10000,
            MaxRetries = 3,
            DistanceTolerance = 0.15
        };

        await client.GetRouteAsync(request);

        // 1 initial + 3 retries = 4 total attempts
        capturedUris.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetRouteAsync_RoundTrip_StopsRetryingWhenDistanceWithinTolerance()
    {
        var capturedUris = new List<Uri>();
        // First attempt: 5000m (50% off from 10000m — retries); second attempt: 9500m (5% off — within 15%)
        var sequenceHandler = new SequenceHandler(capturedUris,
        [
            new RouteResponse { Paths = [new RoutePath { Distance = 5000 }] },
            new RouteResponse { Paths = [new RoutePath { Distance = 9500 }] }
        ]);
        var httpClient = new HttpClient(sequenceHandler) { BaseAddress = new Uri("http://graphhopper") };
        var client = new GraphHopperClient(httpClient, NullLogger<GraphHopperClient>.Instance);

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10000,
            MaxRetries = 3,
            DistanceTolerance = 0.15
        };

        var result = await client.GetRouteAsync(request);

        // Should stop after 2 attempts once tolerance is met
        capturedUris.Should().HaveCount(2);
        result!.Paths[0].Distance.Should().BeApproximately(9500, 1);
    }

    [Fact]
    public async Task GetRouteAsync_RoundTrip_SeedDiffersBetweenRetries()
    {
        var capturedUris = new List<Uri>();
        var response = new RouteResponse { Paths = [new RoutePath { Distance = 5000 }] };
        var handler = new CapturingHandler(capturedUris, JsonSerializer.Serialize(response));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://graphhopper") };
        var client = new GraphHopperClient(httpClient, NullLogger<GraphHopperClient>.Instance);

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10000,
            MaxRetries = 2,
            DistanceTolerance = 0.15
        };

        await client.GetRouteAsync(request);

        // Extract seeds from each captured URI and verify they differ
        var seeds = capturedUris
            .Select(u => System.Text.RegularExpressions.Regex.Match(u.ToString(), @"round_trip\.seed=(\d+)").Groups[1].Value)
            .ToList();

        seeds.Should().HaveCount(3);
        seeds.Distinct().Should().HaveCountGreaterThan(1, "each retry should use a different seed");
    }

    [Fact]
    public async Task GetRouteAsync_RoundTrip_NoRetryWhenFirstAttemptWithinTolerance()
    {
        var capturedUris = new List<Uri>();
        // 9500m is 5% off from 10000m — within the 15% default tolerance
        var response = new RouteResponse { Paths = [new RoutePath { Distance = 9500 }] };
        var handler = new CapturingHandler(capturedUris, JsonSerializer.Serialize(response));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://graphhopper") };
        var client = new GraphHopperClient(httpClient, NullLogger<GraphHopperClient>.Instance);

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10000,
            MaxRetries = 3,
            DistanceTolerance = 0.15
        };

        var result = await client.GetRouteAsync(request);

        // First attempt lands within tolerance — no retry should be issued
        capturedUris.Should().HaveCount(1, "route was within tolerance on first attempt, no retry needed");
        result!.Paths[0].Distance.Should().BeApproximately(9500, 1);
    }

    [Fact]
    public async Task GetRouteAsync_RoundTrip_RespectsCustomTolerance()
    {
        var capturedUris = new List<Uri>();
        // 9400m is 6% off from 10000m — within 15% but outside the tight 5% tolerance
        // 9800m is 2% off from 10000m — within the 5% tolerance
        var responses = new[]
        {
            new RouteResponse { Paths = [new RoutePath { Distance = 9400 }] },
            new RouteResponse { Paths = [new RoutePath { Distance = 9800 }] }
        };
        var handler = new SequenceHandler(capturedUris, responses);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://graphhopper") };
        var client = new GraphHopperClient(httpClient, NullLogger<GraphHopperClient>.Instance);

        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10000,
            MaxRetries = 3,
            DistanceTolerance = 0.05 // tight 5% tolerance
        };

        var result = await client.GetRouteAsync(request);

        // 9400m (6% off) exceeds 5% tolerance → retry; 9800m (2% off) is within → stop
        capturedUris.Should().HaveCount(2, "first response exceeded 5% tolerance, one retry was needed");
        result!.Paths[0].Distance.Should().BeApproximately(9800, 1);
    }

    [Fact]
    public async Task GetRouteAsync_AbRoute_DoesNotRetryRegardlessOfDistance()
    {
        var capturedUris = new List<Uri>();
        // Return a distance that would be far out of tolerance for a round-trip
        var response = new RouteResponse { Paths = [new RoutePath { Distance = 999 }] };
        var handler = new CapturingHandler(capturedUris, JsonSerializer.Serialize(response));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://graphhopper") };
        var client = new GraphHopperClient(httpClient, NullLogger<GraphHopperClient>.Instance);

        // Explicitly set MaxRetries and DistanceTolerance to show A→B ignores them
        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            To = [53.4808, -2.2426],
            MaxRetries = 3,
            DistanceTolerance = 0.15
        };

        var result = await client.GetRouteAsync(request);

        // A→B routing takes the direct path — retry logic is never entered
        capturedUris.Should().HaveCount(1, "A→B routing does not use retry logic");
        result!.Paths[0].Distance.Should().BeApproximately(999, 1);
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

/// <summary>
/// DelegatingHandler that returns responses from a pre-defined sequence, then repeats the last.
/// </summary>
internal class SequenceHandler(List<Uri> capturedUris, RouteResponse[] responses) : HttpMessageHandler
{
    private int _callIndex;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        capturedUris.Add(request.RequestUri!);
        var response = responses[Math.Min(_callIndex++, responses.Length - 1)];
        var json = JsonSerializer.Serialize(response);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        return Task.FromResult(httpResponse);
    }
}
