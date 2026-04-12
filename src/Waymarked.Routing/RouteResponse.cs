using System.Text.Json.Serialization;

namespace Waymarked.Routing;

public record RouteResponse
{
    [JsonPropertyName("paths")]
    public RoutePath[] Paths { get; init; } = [];
}

public record RoutePath
{
    [JsonPropertyName("distance")]
    public double Distance { get; init; }
    
    [JsonPropertyName("time")]
    public long Time { get; init; }
    
    [JsonPropertyName("ascend")]
    public double? Ascend { get; init; }
    
    [JsonPropertyName("descend")]
    public double? Descend { get; init; }
    
    [JsonPropertyName("points")]
    public RoutePoints? Points { get; init; }
    
    [JsonPropertyName("instructions")]
    public RouteInstruction[]? Instructions { get; init; }
    
    [JsonPropertyName("snapped_waypoints")]
    public RoutePoints? SnappedWaypoints { get; init; }
}

public record RoutePoints
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "LineString";
    
    [JsonPropertyName("coordinates")]
    public double[][]? Coordinates { get; init; }
}

public record RouteInstruction
{
    [JsonPropertyName("distance")]
    public double Distance { get; init; }
    
    [JsonPropertyName("time")]
    public long Time { get; init; }
    
    [JsonPropertyName("text")]
    public string? Text { get; init; }
    
    [JsonPropertyName("street_name")]
    public string? StreetName { get; init; }
}
