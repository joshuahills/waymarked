namespace Waymarked.Routing;

public enum DistanceUnit
{
    Kilometres,
    Miles
}

public record RouteRequest
{
    public required double[] From { get; init; }
    public double[]? To { get; init; }
    public double? Distance { get; init; }
    public DistanceUnit DistanceUnit { get; init; } = DistanceUnit.Kilometres;
    public string Profile { get; init; } = "hike";
    public string Locale { get; init; } = "en-GB";
    public bool Instructions { get; init; } = true;
    public bool CalcPoints { get; init; } = true;
    public bool Elevation { get; init; } = true;
    public bool PointsEncoded { get; init; } = false;
}
