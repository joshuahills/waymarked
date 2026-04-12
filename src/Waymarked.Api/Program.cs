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
    // Validate From
    if (apiRequest.From == null || apiRequest.From.Length < 2)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["From"] = ["From is required and must contain at least latitude and longitude"]
        });
    }

    // Validate round-trip requirements
    if (apiRequest.To == null && (apiRequest.Distance == null || apiRequest.Distance <= 0))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Distance"] = ["Distance is required for round-trip routes"]
        });
    }

    // Convert distance from km/miles to metres
    double? distanceInMetres = null;
    if (apiRequest.Distance.HasValue)
    {
        var unit = apiRequest.DistanceUnit?.ToLowerInvariant() ?? "kilometres";
        distanceInMetres = unit switch
        {
            "miles" => apiRequest.Distance.Value * 1609.344,
            "kilometres" or "kilometers" => apiRequest.Distance.Value * 1000,
            _ => apiRequest.Distance.Value * 1000 // default to kilometres
        };
    }

    // Validate distance bounds for round-trip routes
    if (distanceInMetres.HasValue)
    {
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

    // Validate coordinates are within Great Britain
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

    var request = new RouteRequest
    {
        From = apiRequest.From,
        To = apiRequest.To,
        Distance = distanceInMetres,
        Profile = apiRequest.Profile ?? "hike"
    };

    try
    {
        var response = await client.GetRouteAsync(request);
        
        if (response == null || response.Paths.Length == 0)
        {
            return Results.NotFound(new { error = "No route found" });
        }

        var waymarkedResponse = WaymarkedRouteResponse.FromRouteResponse(response, isRoundTrip: apiRequest.To == null);
        return Results.Ok(waymarkedResponse);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway,
            title: "Failed to communicate with routing engine"
        );
    }
})
.WithName("PlanRoute")
.WithOpenApi();

app.MapGet("/api/bounds", () => Results.Ok(new
{
    minLat = 49.8,
    maxLat = 60.9,
    minLon = -8.7,
    maxLon = 1.8
})).WithName("GetBounds");

app.MapDefaultEndpoints();

app.Run();

record ApiRouteRequest(
    double[] From,
    double[]? To = null,
    double? Distance = null,
    string? DistanceUnit = "kilometres",
    string? Profile = "hike"
);

public partial class Program { }
