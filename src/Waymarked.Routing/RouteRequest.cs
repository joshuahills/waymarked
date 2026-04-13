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

    /// <summary>
    /// Maximum number of client-side retries with a different seed when the returned
    /// round-trip distance deviates beyond <see cref="DistanceTolerance"/>.
    /// Zero means no retry (single attempt). Only applies to round-trip mode.
    /// </summary>
    public int MaxRetries { get; init; } = 0;

    /// <summary>
    /// Acceptable fractional deviation between the requested and returned round-trip
    /// distance before a retry is triggered. Default 0.15 = 15%.
    /// </summary>
    public double DistanceTolerance { get; init; } = 0.15;
}
