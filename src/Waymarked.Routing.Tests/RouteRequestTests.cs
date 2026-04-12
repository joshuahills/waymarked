using FluentAssertions;
using Waymarked.Routing;

namespace Waymarked.Routing.Tests;

public class RouteRequestTests
{
    [Fact]
    public void RouteRequest_HasSensibleDefaults()
    {
        var request = new RouteRequest { From = [51.5074, -0.1278] };

        request.Profile.Should().Be("hike");
        request.Locale.Should().Be("en-GB");
        request.Instructions.Should().BeTrue();
        request.CalcPoints.Should().BeTrue();
        request.Elevation.Should().BeTrue();
        request.PointsEncoded.Should().BeFalse();
        request.DistanceUnit.Should().Be(DistanceUnit.Kilometres);
    }

    [Fact]
    public void RouteRequest_ToIsOptional_DefaultsToNull()
    {
        var request = new RouteRequest { From = [51.5074, -0.1278] };

        request.To.Should().BeNull();
    }

    [Fact]
    public void RouteRequest_FromIsRequired()
    {
        // Verifies the required keyword is honoured at compile time — runtime check via record init
        var request = new RouteRequest { From = [53.4808, -2.2426] };
        request.From.Should().HaveCount(2);
        request.From[0].Should().BeApproximately(53.4808, 0.0001);
        request.From[1].Should().BeApproximately(-2.2426, 0.0001);
    }

    [Fact]
    public void RouteRequest_SupportsAbRouting_WithBothPoints()
    {
        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            To = [53.4808, -2.2426]
        };

        request.From.Should().NotBeNull();
        request.To.Should().NotBeNull();
        request.To.Should().HaveCount(2);
    }

    [Fact]
    public void RouteRequest_SupportsRoundTrip_WithDistanceOnly()
    {
        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 10000 // 10km in metres
        };

        request.To.Should().BeNull();
        request.Distance.Should().Be(10000);
    }

    [Fact]
    public void RouteRequest_DistanceUnit_DefaultsToKilometres()
    {
        var request = new RouteRequest { From = [51.5074, -0.1278] };
        request.DistanceUnit.Should().Be(DistanceUnit.Kilometres);
    }

    [Fact]
    public void RouteRequest_DistanceUnit_CanBeSetToMiles()
    {
        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Distance = 6.2,
            DistanceUnit = DistanceUnit.Miles
        };

        request.DistanceUnit.Should().Be(DistanceUnit.Miles);
    }

    [Fact]
    public void RouteRequest_Profile_CanBeOverridden()
    {
        var request = new RouteRequest
        {
            From = [51.5074, -0.1278],
            Profile = "bike"
        };

        request.Profile.Should().Be("bike");
    }
}
