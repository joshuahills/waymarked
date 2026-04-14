namespace Waymarked.Api;

using System.Text;
using Waymarked.Routing;

internal static class RouteEndpoints
{
    internal static void MapRouteEndpoints(this WebApplication app, bool elevationEnabled)
    {
        app.MapPost("/api/routes", async (ApiRouteRequest req, GraphHopperClient client) =>
        {
            var validationResult = Validate(req, out var distanceInMetres);
            if (validationResult != null) return validationResult;

            var routeResult = await ExecuteAsync(client, BuildRequest(req, distanceInMetres, elevationEnabled), req.To == null);
            return routeResult.Error ?? Results.Ok(routeResult.Route);
        })
        .WithName("PlanRoute");

        app.MapPost("/api/routes/export/gpx", (WaymarkedRouteResponse route) =>
            Results.File(Encoding.UTF8.GetBytes(RouteExporter.BuildGpx(route)), "application/gpx+xml", "waymarked-route.gpx"))
            .WithName("ExportGpx");

        app.MapPost("/api/routes/export/kml", (WaymarkedRouteResponse route) =>
            Results.File(Encoding.UTF8.GetBytes(RouteExporter.BuildKml(route)), "application/vnd.google-earth.kml+xml", "waymarked-route.kml"))
            .WithName("ExportKml");

        app.MapPost("/api/routes/export/geojson", (WaymarkedRouteResponse route) =>
            Results.File(Encoding.UTF8.GetBytes(RouteExporter.BuildGeoJson(route)), "application/geo+json", "waymarked-route.geojson"))
            .WithName("ExportGeoJson");

        app.MapGet("/api/bounds", () => Results.Ok(new
        {
            minLat = 49.8,
            maxLat = 60.9,
            minLon = -8.7,
            maxLon = 1.8
        }))
        .WithName("GetBounds");
    }

    private static IResult? Validate(ApiRouteRequest req, out double? distanceInMetres)
    {
        distanceInMetres = null;

        if (req.From == null || req.From.Length < 2)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["From"] = ["From is required and must contain at least latitude and longitude"]
            });

        if (req.To == null && (req.Distance == null || req.Distance <= 0))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Distance"] = ["Distance is required for round-trip routes"]
            });

        if (req.Distance.HasValue)
        {
            var unit = req.DistanceUnit?.ToLowerInvariant() ?? "kilometres";
            distanceInMetres = unit switch
            {
                "miles" => req.Distance.Value * 1609.344,
                _ => req.Distance.Value * 1000
            };

            if (distanceInMetres.Value < 500)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Distance"] = ["Distance must be at least 0.5 km (500 metres)"]
                });

            if (distanceInMetres.Value > 100000)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Distance"] = ["Distance must not exceed 100 km (100,000 metres)"]
                });
        }

        if (!UkBoundsValidator.IsWithinBounds(req.From))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["From"] = [UkBoundsValidator.GetOutOfBoundsMessage(req.From)]
            });

        if (req.To != null && !UkBoundsValidator.IsWithinBounds(req.To))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["To"] = [UkBoundsValidator.GetOutOfBoundsMessage(req.To)]
            });

        return null;
    }

    private static RouteRequest BuildRequest(ApiRouteRequest req, double? distanceInMetres, bool elevationEnabled) =>
        new()
        {
            From = req.From,
            To = req.To,
            Distance = distanceInMetres,
            Profile = req.Profile ?? "hike",
            Elevation = elevationEnabled,
            MaxRetries = req.To == null ? 3 : 0,
            DistanceTolerance = req.DistanceTolerance ?? 0.15
        };

    private static async Task<(WaymarkedRouteResponse? Route, IResult? Error)> ExecuteAsync(
        GraphHopperClient client, RouteRequest request, bool isRoundTrip)
    {
        try
        {
            var response = await client.GetRouteAsync(request);

            if (response == null || response.Paths.Length == 0)
                return (null, Results.NotFound(new { error = "No route found" }));

            return (WaymarkedRouteResponse.FromRouteResponse(response, isRoundTrip), null);
        }
        catch (HttpRequestException ex)
        {
            return (null, Results.Problem(
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway,
                title: "Failed to communicate with routing engine"
            ));
        }
    }
}

record ApiRouteRequest(
    double[] From,
    double[]? To = null,
    double? Distance = null,
    string? DistanceUnit = "kilometres",
    string? Profile = "hike",
    double? DistanceTolerance = null
);
