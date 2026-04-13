using System.Text;
using Waymarked.Routing;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add GraphHopper client - uses Aspire service discovery (resolves "graphhopper" to the container endpoint)
builder.Services.AddGraphHopperClient();

// In CI the routing graph is built without elevation data (faster graph build, no SRTM downloads).
// GRAPHHOPPER__ELEVATIONENABLED is set to "false" by the AppHost when using a pre-built CI image.
var elevationEnabled = builder.Configuration.GetValue<bool>("GRAPHHOPPER:ELEVATIONENABLED", true);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/", () => "Waymarked API is running. POST to /api/routes to plan a route.");

app.MapPost("/api/routes", async (ApiRouteRequest apiRequest, GraphHopperClient client) =>
{
    var validationResult = ValidateRouteRequest(apiRequest, out var distanceInMetres);
    if (validationResult != null) return validationResult;

    var request = BuildRouteRequest(apiRequest, distanceInMetres, elevationEnabled);
    var routeResult = await ExecuteRouteAsync(client, request, apiRequest.To == null);
    if (routeResult.Error != null) return routeResult.Error;

    return Results.Ok(routeResult.Route);
})
.WithName("PlanRoute")
.WithOpenApi();

// ─── Export endpoints ────────────────────────────────────────────────────────

app.MapPost("/api/routes/export/gpx", async (ApiRouteRequest apiRequest, GraphHopperClient client) =>
{
    var validationResult = ValidateRouteRequest(apiRequest, out var distanceInMetres);
    if (validationResult != null) return validationResult;

    var request = BuildRouteRequest(apiRequest, distanceInMetres, elevationEnabled);
    var routeResult = await ExecuteRouteAsync(client, request, apiRequest.To == null);
    if (routeResult.Error != null) return routeResult.Error;

    var gpx = RouteExporter.BuildGpx(routeResult.Route!);
    var bytes = Encoding.UTF8.GetBytes(gpx);
    return Results.File(bytes, "application/gpx+xml", "waymarked-route.gpx");
})
.WithName("ExportGpx")
.WithOpenApi();

app.MapPost("/api/routes/export/kml", async (ApiRouteRequest apiRequest, GraphHopperClient client) =>
{
    var validationResult = ValidateRouteRequest(apiRequest, out var distanceInMetres);
    if (validationResult != null) return validationResult;

    var request = BuildRouteRequest(apiRequest, distanceInMetres, elevationEnabled);
    var routeResult = await ExecuteRouteAsync(client, request, apiRequest.To == null);
    if (routeResult.Error != null) return routeResult.Error;

    var kml = RouteExporter.BuildKml(routeResult.Route!);
    var bytes = Encoding.UTF8.GetBytes(kml);
    return Results.File(bytes, "application/vnd.google-earth.kml+xml", "waymarked-route.kml");
})
.WithName("ExportKml")
.WithOpenApi();

app.MapPost("/api/routes/export/geojson", async (ApiRouteRequest apiRequest, GraphHopperClient client) =>
{
    var validationResult = ValidateRouteRequest(apiRequest, out var distanceInMetres);
    if (validationResult != null) return validationResult;

    var request = BuildRouteRequest(apiRequest, distanceInMetres, elevationEnabled);
    var routeResult = await ExecuteRouteAsync(client, request, apiRequest.To == null);
    if (routeResult.Error != null) return routeResult.Error;

    var geoJson = RouteExporter.BuildGeoJson(routeResult.Route!);
    var bytes = Encoding.UTF8.GetBytes(geoJson);
    return Results.File(bytes, "application/geo+json", "waymarked-route.geojson");
})
.WithName("ExportGeoJson")
.WithOpenApi();

// ─────────────────────────────────────────────────────────────────────────────

app.MapGet("/api/bounds", () => Results.Ok(new
{
    minLat = 49.8,
    maxLat = 60.9,
    minLon = -8.7,
    maxLon = 1.8
})).WithName("GetBounds");

app.MapDefaultEndpoints();

app.Run();

// ─── Shared helpers ──────────────────────────────────────────────────────────

/// <summary>
/// Validates the route request. Returns an IResult error if invalid, null if valid.
/// Also outputs the pre-converted distance in metres (null for A→B routes).
/// </summary>
static IResult? ValidateRouteRequest(ApiRouteRequest apiRequest, out double? distanceInMetres)
{
    distanceInMetres = null;

    if (apiRequest.From == null || apiRequest.From.Length < 2)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["From"] = ["From is required and must contain at least latitude and longitude"]
        });
    }

    if (apiRequest.To == null && (apiRequest.Distance == null || apiRequest.Distance <= 0))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Distance"] = ["Distance is required for round-trip routes"]
        });
    }

    if (apiRequest.Distance.HasValue)
    {
        var unit = apiRequest.DistanceUnit?.ToLowerInvariant() ?? "kilometres";
        distanceInMetres = unit switch
        {
            "miles" => apiRequest.Distance.Value * 1609.344,
            "kilometres" or "kilometers" => apiRequest.Distance.Value * 1000,
            _ => apiRequest.Distance.Value * 1000
        };

        if (distanceInMetres.Value < 500)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Distance"] = ["Distance must be at least 0.5 km (500 metres)"]
            });
        }

        if (distanceInMetres.Value > 100000)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Distance"] = ["Distance must not exceed 100 km (100,000 metres)"]
            });
        }
    }

    if (!UkBoundsValidator.IsWithinBounds(apiRequest.From))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["From"] = [UkBoundsValidator.GetOutOfBoundsMessage(apiRequest.From)]
        });
    }

    if (apiRequest.To != null && !UkBoundsValidator.IsWithinBounds(apiRequest.To))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["To"] = [UkBoundsValidator.GetOutOfBoundsMessage(apiRequest.To)]
        });
    }

    return null;
}

static RouteRequest BuildRouteRequest(ApiRouteRequest apiRequest, double? distanceInMetres, bool elevationEnabled) =>
    new()
    {
        From = apiRequest.From,
        To = apiRequest.To,
        Distance = distanceInMetres,
        Profile = apiRequest.Profile ?? "hike",
        Elevation = elevationEnabled,
        MaxRetries = apiRequest.To == null ? 3 : 0,
        DistanceTolerance = apiRequest.DistanceTolerance ?? 0.15
    };

static async Task<(WaymarkedRouteResponse? Route, IResult? Error)> ExecuteRouteAsync(
    GraphHopperClient client, RouteRequest request, bool isRoundTrip)
{
    try
    {
        var response = await client.GetRouteAsync(request);

        if (response == null || response.Paths.Length == 0)
            return (null, Results.NotFound(new { error = "No route found" }));

        var route = WaymarkedRouteResponse.FromRouteResponse(response, isRoundTrip);
        return (route, null);
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

// ─────────────────────────────────────────────────────────────────────────────

record ApiRouteRequest(
    double[] From,
    double[]? To = null,
    double? Distance = null,
    string? DistanceUnit = "kilometres",
    string? Profile = "hike",
    double? DistanceTolerance = null
);

public partial class Program { }
