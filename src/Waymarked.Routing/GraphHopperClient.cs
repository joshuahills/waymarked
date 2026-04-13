using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Waymarked.Routing;

public class GraphHopperClient(HttpClient httpClient, ILogger<GraphHopperClient> logger)
{
    public async Task<RouteResponse?> GetRouteAsync(RouteRequest request, CancellationToken cancellationToken = default)
    {
        var baseQueryString = string.Join("&", new Dictionary<string, string>
        {
            ["point"] = $"{request.From[0]},{request.From[1]}",
            ["profile"] = request.Profile,
            ["locale"] = request.Locale,
            ["instructions"] = request.Instructions.ToString().ToLowerInvariant(),
            ["calc_points"] = request.CalcPoints.ToString().ToLowerInvariant(),
            ["elevation"] = request.Elevation.ToString().ToLowerInvariant(),
            ["points_encoded"] = request.PointsEncoded.ToString().ToLowerInvariant()
        }.Select(kvp => $"{kvp.Key}={kvp.Value}"));

        if (request.To != null)
        {
            // A→B routing: add destination point
            var requestUri = $"/route?{baseQueryString}&point={request.To[0]},{request.To[1]}";
            logger.LogInformation("Requesting A→B route from GraphHopper: {RequestUri}", requestUri);
            return await SendAsync(requestUri, cancellationToken);
        }

        // Round-trip routing with client-side retry on distance deviation.
        // ch.disable=true required — Contraction Hierarchies doesn't support round_trip algorithm.
        // round_trip.max_retries tells GraphHopper how many internal attempts to make per call.
        logger.LogInformation("Using round-trip routing mode with distance {Distance}m (max client retries: {MaxRetries})",
            request.Distance, request.MaxRetries);

        var requestedDistance = request.Distance!.Value;
        var maxAttempts = request.MaxRetries + 1;
        RouteResponse? bestResponse = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var seed = Random.Shared.Next();
            var roundTripUri = $"/route?{baseQueryString}"
                + "&ch.disable=true"
                + "&algorithm=round_trip"
                + $"&round_trip.distance={Convert.ToInt32(requestedDistance)}"
                + $"&round_trip.seed={seed}"
                + "&round_trip.max_retries=10";

            logger.LogInformation("Round-trip attempt {Attempt}/{MaxAttempts} (seed {Seed})",
                attempt, maxAttempts, seed);

            var response = await SendAsync(roundTripUri, cancellationToken);
            bestResponse = response;

            // No retry configured, or no usable path returned — return whatever we got
            if (request.MaxRetries == 0 || response == null || response.Paths.Length == 0)
                break;

            var returnedDistance = response.Paths[0].Distance;
            var deviation = Math.Abs(returnedDistance - requestedDistance) / requestedDistance;

            logger.LogInformation(
                "Round-trip attempt {Attempt}: requested {Requested}m, got {Returned}m (deviation {Deviation:P1})",
                attempt, requestedDistance, returnedDistance, deviation);

            if (deviation <= request.DistanceTolerance)
            {
                logger.LogInformation("Round-trip within {Tolerance:P0} tolerance on attempt {Attempt}",
                    request.DistanceTolerance, attempt);
                break;
            }

            if (attempt < maxAttempts)
            {
                logger.LogWarning(
                    "Round-trip deviation {Deviation:P1} exceeds {Tolerance:P0} tolerance — retrying with different seed",
                    deviation, request.DistanceTolerance);
            }
        }

        return bestResponse;
    }

    private async Task<RouteResponse?> SendAsync(string requestUri, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetAsync(requestUri, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<RouteResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to get route from GraphHopper");
            throw;
        }
    }
}
