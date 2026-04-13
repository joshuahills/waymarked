namespace Waymarked.Routing.Tests;

using FluentAssertions;
using Waymarked.Routing;

/// <summary>
/// Unit tests for <see cref="RouteExporter"/> — GPX, KML, and GeoJSON output.
///
/// Key coordinate convention: <see cref="RoutePoints.Coordinates"/> are stored in
/// GeoJSON order [longitude, latitude].  BuildGpx must swap them to lat/lon;
/// BuildKml writes them lon,lat (no swap needed); BuildGeoJson passes them through.
/// </summary>
public class RouteExporterTests
{
    // ─── Sample fixture ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal but realistic <see cref="WaymarkedRouteResponse"/> for testing.
    /// Coordinates are in GeoJSON order: [lon, lat] = [-3.1367, 54.5994].
    /// </summary>
    private static WaymarkedRouteResponse BuildSampleRoute() => new()
    {
        DistanceKm = 5.5,
        DistanceMiles = 3.4,
        DurationFormatted = "1h 10m",
        IsRoundTrip = true,
        Points = new RoutePoints
        {
            Type = "LineString",
            Coordinates = [[-3.1367, 54.5994], [-3.14, 54.61]]
        },
        Instructions =
        [
            new RouteInstruction
            {
                Text     = "Head north",
                Distance = 500,
                Sign     = 0,
                Interval = [0, 5]
            }
        ]
    };

    // ─── GPX ─────────────────────────────────────────────────────────────────

    [Fact]
    public void BuildGpx_ContainsTrkptElements()
    {
        var route = BuildSampleRoute();

        var gpx = RouteExporter.BuildGpx(route);

        gpx.Should().Contain("<trkpt",
            "every coordinate must produce a GPX track-point element");
    }

    [Fact]
    public void BuildGpx_CorrectlySwapsLonLatToLatLon()
    {
        // Input coord: [-3.1367, 54.5994] — GeoJSON order (lon, lat)
        // GPX requires lat="54.5994" lon="-3.1367"  (lat first in the attribute)
        var route = BuildSampleRoute();

        var gpx = RouteExporter.BuildGpx(route);

        // F6 formatting: 54.5994 → "54.599400", -3.1367 → "-3.136700"
        gpx.Should().Contain("lat=\"54.599400\"",
            "BuildGpx must place coord[1] (latitude) in the lat attribute");
        gpx.Should().Contain("lon=\"-3.136700\"",
            "BuildGpx must place coord[0] (longitude) in the lon attribute");

        // Verify the swap is correct — lat value must NOT appear in lon and vice-versa
        gpx.Should().NotContain("lat=\"-3.136700\"",
            "the longitude value must not appear in the lat attribute");
        gpx.Should().NotContain("lon=\"54.599400\"",
            "the latitude value must not appear in the lon attribute");
    }

    [Fact]
    public void BuildGpx_ContainsMetadataWithDistance()
    {
        var route = BuildSampleRoute();

        var gpx = RouteExporter.BuildGpx(route);

        // DistanceKm = 5.5, formatted as "5.50" via F2
        gpx.Should().Contain("5.5",
            "the GPX metadata description must include the route distance");
    }

    // ─── KML ─────────────────────────────────────────────────────────────────

    [Fact]
    public void BuildKml_ContainsLineStringElement()
    {
        var route = BuildSampleRoute();

        var kml = RouteExporter.BuildKml(route);

        kml.Should().Contain("<LineString>",
            "KML route geometry must be expressed as a LineString placemark");
    }

    [Fact]
    public void BuildKml_CoordinatesInLonLatOrder()
    {
        // KML expects lon,lat,altitude — same order as GeoJSON input, so no swap.
        // For coord [-3.1367, 54.5994] the output should be "-3.136700,54.599400,0"
        var route = BuildSampleRoute();

        var kml = RouteExporter.BuildKml(route);

        kml.Should().Contain("-3.136700,54.599400",
            "KML coordinates must be in lon,lat order (matching GeoJSON input — no swap)");

        // Confirm the order is NOT swapped (i.e. lat first would be wrong)
        kml.Should().NotContain("54.599400,-3.136700",
            "KML must not swap to lat,lon order");
    }

    // ─── GeoJSON ─────────────────────────────────────────────────────────────

    [Fact]
    public void BuildGeoJson_IsFeatureCollection()
    {
        var route = BuildSampleRoute();

        var geoJson = RouteExporter.BuildGeoJson(route);

        geoJson.Should().Contain("FeatureCollection",
            "the top-level GeoJSON type must be FeatureCollection");
    }

    [Fact]
    public void BuildGeoJson_ContainsDistanceKm()
    {
        // DistanceKm = 5.5 — serialised as the JSON number 5.5
        var route = BuildSampleRoute();

        var geoJson = RouteExporter.BuildGeoJson(route);

        geoJson.Should().Contain("5.5",
            "GeoJSON properties must include the distanceKm value");
    }
}
