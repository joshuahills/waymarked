using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Waymarked.Routing;

public class GraphHopperClient(HttpClient httpClient, ILogger<GraphHopperClient> logger)
{
    public async Task<RouteResponse?> GetRouteAsync(RouteRequest request, CancellationToken cancellationToken = default)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["point"] = $"{request.From[0]},{request.From[1]}",
            ["profile"] = request.Profile,
            ["locale"] = request.Locale,
            ["instructions"] = request.Instructions.ToString().ToLowerInvariant(),
            ["calc_points"] = request.CalcPoints.ToString().ToLowerInvariant(),
            ["elevation"] = request.Elevation.ToString().ToLowerInvariant(),
            ["points_encoded"] = request.PointsEncoded.ToString().ToLowerInvariant()
        };

        var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        queryString += $"&point={request.To[0]},{request.To[1]}";

        var requestUri = $"/route?{queryString}";
        
        logger.LogInformation("Requesting route from GraphHopper: {RequestUri}", requestUri);

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
