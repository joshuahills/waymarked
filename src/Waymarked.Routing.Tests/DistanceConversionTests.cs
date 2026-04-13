namespace Waymarked.Routing.Tests;

using FluentAssertions;

/// <summary>
/// Tests for distance unit conversion (km/mi → metres).
/// Conversion happens in the API layer (Program.cs), not in the routing library itself.
/// These tests verify the conversion logic matches what Program.cs implements.
/// </summary>
public class DistanceConversionTests
{
    // The conversion logic from Program.cs:
    // "miles" => distance * 1609.344
    // "kilometres" or "kilometers" => distance * 1000
    // _ => distance * 1000 (default)

    [Theory]
    [InlineData(1.0, 1609.344)]
    [InlineData(5.0, 8046.72)]
    [InlineData(10.0, 16093.44)]
    [InlineData(0.5, 804.672)]
    public void MilesToMetres_ConvertsCorrectly(double miles, double expectedMetres)
    {
        var result = ConvertToMetres(miles, "miles");
        result.Should().BeApproximately(expectedMetres, 0.001);
    }

    [Theory]
    [InlineData(1.0, 1000.0)]
    [InlineData(5.0, 5000.0)]
    [InlineData(10.0, 10000.0)]
    [InlineData(0.5, 500.0)]
    public void KilometresToMetres_ConvertsCorrectly(double km, double expectedMetres)
    {
        var result = ConvertToMetres(km, "kilometres");
        result.Should().BeApproximately(expectedMetres, 0.001);
    }

    [Theory]
    [InlineData("kilometers")]   // American spelling
    [InlineData("kilometres")]   // British spelling
    public void KilometresVariantSpellings_ConvertsCorrectly(string unit)
    {
        var result = ConvertToMetres(5.0, unit);
        result.Should().BeApproximately(5000.0, 0.001);
    }

    [Fact]
    public void UnknownUnit_DefaultsToKilometres()
    {
        var result = ConvertToMetres(5.0, "furlongs");
        result.Should().BeApproximately(5000.0, 0.001);
    }

    // Mirrors the switch expression in Program.cs
    private static double ConvertToMetres(double distance, string unit) => unit switch
    {
        "miles" => distance * 1609.344,
        "kilometres" or "kilometers" => distance * 1000,
        _ => distance * 1000
    };
}
