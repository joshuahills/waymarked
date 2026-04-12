namespace Waymarked.Routing;

public record RouteRequest
{
    public required double[] From { get; init; }
    public required double[] To { get; init; }
    public string Profile { get; init; } = "hike";
    public string Locale { get; init; } = "en-GB";
    public bool Instructions { get; init; } = true;
    public bool CalcPoints { get; init; } = true;
    public bool Elevation { get; init; } = true;
    public bool PointsEncoded { get; init; } = false;
}
