using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Waymarked.Routing;

/// <summary>
/// Converts a <see cref="WaymarkedRouteResponse"/> into GPX, KML, or GeoJSON bytes
/// ready to serve as a file download.
/// </summary>
public static class RouteExporter
{
    // ─── GPX ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a GPX 1.1 track document from the route.
    /// Coordinates in <see cref="RoutePoints"/> are [longitude, latitude] (GeoJSON order).
    /// GPX needs lat= lon= so we swap: coord[1] = lat, coord[0] = lon.
    /// </summary>
    public static string BuildGpx(WaymarkedRouteResponse route)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<gpx version=\"1.1\" creator=\"Waymarked - UK Route Planner\"");
        sb.AppendLine("     xmlns=\"http://www.topografix.com/GPX/1/1\"");
        sb.AppendLine("     xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        sb.AppendLine("     xsi:schemaLocation=\"http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd\">");
        sb.AppendLine("  <metadata>");
        sb.AppendLine("    <name>Waymarked Route</name>");
        sb.AppendLine($"    <desc>{Esc(route.DistanceKm.ToString("F2", CultureInfo.InvariantCulture))}km · {Esc(route.DurationFormatted)}</desc>");
        sb.AppendLine("  </metadata>");
        sb.AppendLine("  <trk>");
        sb.AppendLine("    <name>Waymarked Route</name>");
        sb.AppendLine("    <trkseg>");

        if (route.Points?.Coordinates != null)
        {
            foreach (var coord in route.Points.Coordinates)
            {
                if (coord.Length < 2) continue;
                var lon = coord[0].ToString("F6", CultureInfo.InvariantCulture);
                var lat = coord[1].ToString("F6", CultureInfo.InvariantCulture);
                sb.AppendLine($"      <trkpt lat=\"{lat}\" lon=\"{lon}\"></trkpt>");
            }
        }

        sb.AppendLine("    </trkseg>");
        sb.AppendLine("  </trk>");
        sb.AppendLine("</gpx>");

        return sb.ToString();
    }

    // ─── KML ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a KML 2.2 LineString document from the route.
    /// KML coordinates are lon,lat,altitude — same order as GeoJSON, so no swap needed.
    /// </summary>
    public static string BuildKml(WaymarkedRouteResponse route)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
        sb.AppendLine("  <Document>");
        sb.AppendLine("    <name>Waymarked Route</name>");
        sb.AppendLine($"    <description>{Esc(route.DistanceKm.ToString("F2", CultureInfo.InvariantCulture))}km · {Esc(route.DurationFormatted)}</description>");
        sb.AppendLine("    <Style id=\"routeStyle\">");
        sb.AppendLine("      <LineStyle>");
        sb.AppendLine("        <color>ff275a27</color>");
        sb.AppendLine("        <width>4</width>");
        sb.AppendLine("      </LineStyle>");
        sb.AppendLine("    </Style>");
        sb.AppendLine("    <Placemark>");
        sb.AppendLine("      <name>Route</name>");
        sb.AppendLine("      <styleUrl>#routeStyle</styleUrl>");
        sb.AppendLine("      <LineString>");
        sb.AppendLine("        <tessellate>1</tessellate>");
        sb.AppendLine("        <coordinates>");
        sb.Append("          ");

        if (route.Points?.Coordinates != null)
        {
            var parts = route.Points.Coordinates
                .Where(c => c.Length >= 2)
                .Select(c =>
                    $"{c[0].ToString("F6", CultureInfo.InvariantCulture)}," +
                    $"{c[1].ToString("F6", CultureInfo.InvariantCulture)},0");
            sb.Append(string.Join(" ", parts));
        }

        sb.AppendLine();
        sb.AppendLine("        </coordinates>");
        sb.AppendLine("      </LineString>");
        sb.AppendLine("    </Placemark>");
        sb.AppendLine("  </Document>");
        sb.AppendLine("</kml>");

        return sb.ToString();
    }

    // ─── GeoJSON ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a GeoJSON FeatureCollection with a single LineString feature.
    /// Coordinates are already in [lon, lat] order — passed through as-is.
    /// </summary>
    public static string BuildGeoJson(WaymarkedRouteResponse route)
    {
        var coordinates = route.Points?.Coordinates ?? [];

        var featureCollection = new
        {
            type = "FeatureCollection",
            features = new[]
            {
                new
                {
                    type = "Feature",
                    geometry = new
                    {
                        type = "LineString",
                        coordinates
                    },
                    properties = new
                    {
                        name = "Waymarked Route",
                        distanceKm = route.DistanceKm,
                        distanceMiles = route.DistanceMiles,
                        duration = route.DurationFormatted,
                        isRoundTrip = route.IsRoundTrip
                    }
                }
            }
        };

        return JsonSerializer.Serialize(featureCollection, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Minimal XML character escaping for attribute values and text nodes.</summary>
    private static string Esc(string value) =>
        value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
}
