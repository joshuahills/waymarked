namespace Waymarked.Routing;

public static class UkBoundsValidator
{
    // Bounding box covering mainland GB + major islands (not Ireland)
    // Lat: 49.8° (Lizard Point) to 60.9° (Shetland)
    // Lon: -8.7° (west Ireland coast — generous for NW Scotland) to 1.8° (east England)
    private const double MinLat = 49.8;
    private const double MaxLat = 60.9;
    private const double MinLon = -8.7;
    private const double MaxLon = 1.8;

    public static bool IsWithinBounds(double lat, double lon)
        => lat >= MinLat && lat <= MaxLat && lon >= MinLon && lon <= MaxLon;

    public static bool IsWithinBounds(double[] point)
        => point.Length >= 2 && IsWithinBounds(point[0], point[1]);

    public static string GetOutOfBoundsMessage(double[] point)
        => $"Coordinate ({point[0]:F4}, {point[1]:F4}) is outside Great Britain. " +
           $"Waymarked covers mainland GB and major islands " +
           $"(lat {MinLat}°–{MaxLat}°, lon {MinLon}°–{MaxLon}°).";
}
