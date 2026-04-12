using Waymarked.Routing;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add GraphHopper client
var graphHopperUrl = builder.Configuration.GetConnectionString("graphhopper") 
    ?? throw new InvalidOperationException("GraphHopper connection string not found");
builder.Services.AddGraphHopperClient(graphHopperUrl);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "Waymarked API is running. POST to /api/routes to plan a route.");

app.MapPost("/api/routes", async (RouteRequest request, GraphHopperClient client) =>
{
    try
    {
        var response = await client.GetRouteAsync(request);
        
        if (response == null || response.Paths.Length == 0)
        {
            return Results.NotFound(new { error = "No route found" });
        }

        return Results.Ok(response);
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

app.MapDefaultEndpoints();

app.Run();
