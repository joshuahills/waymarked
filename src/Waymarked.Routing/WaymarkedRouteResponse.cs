namespace Waymarked.Routing;

public record WaymarkedRouteResponse
{
    public double Distance { get; init; }
    public double DistanceKm { get; init; }
    public double DistanceMiles { get; init; }
    public long DurationMs { get; init; }
    public string DurationFormatted { get; init; } = string.Empty;
    public RoutePoints? Points { get; init; }
    public RouteInstruction[]? Instructions { get; init; }
    public bool IsRoundTrip { get; init; }

    public static WaymarkedRouteResponse FromRouteResponse(RouteResponse response, bool isRoundTrip)
    {
        var firstPath = response.Paths[0];
        
        var distanceKm = Math.Round(firstPath.Distance / 1000, 2);
        var distanceMiles = Math.Round(firstPath.Distance / 1609.344, 2);
        
        var durationFormatted = FormatDuration(firstPath.Time);

        return new WaymarkedRouteResponse
        {
            Distance = firstPath.Distance,
            DistanceKm = distanceKm,
            DistanceMiles = distanceMiles,
            DurationMs = firstPath.Time,
            DurationFormatted = durationFormatted,
            Points = firstPath.Points,
            Instructions = firstPath.Instructions,
            IsRoundTrip = isRoundTrip
        };
    }

    private static string FormatDuration(long milliseconds)
    {
        var totalMinutes = (int)Math.Round(milliseconds / 60000.0);
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;

        if (hours > 0)
        {
            return $"{hours}h {minutes}m";
        }
        
        return $"{minutes}m";
    }
}
