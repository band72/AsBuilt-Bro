using System;
using System.IO;
using System.Text;
using RCS.Geo.Core;

namespace RCS.Geo.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// StatePlaneProjectionTests — forward (SP→LatLon) and inverse (LatLon→SP)
// ─────────────────────────────────────────────────────────────────────────────
public class StatePlaneProjectionTests
{
    // ── Accuracy and range checks ─────────────────────────────────────────────
    // Central meridian = -81°, so at central meridian the forward transform
    // produces no easting distortion.  We test math consistency via round-trips.

    // Florida State Plane East zone covers latitudes ~24.3° – 31° N,
    // longitudes roughly -82° – -80° W.
    // Used NAD83 EPSG:2236 reference point:
    //   POB: lat=30.0000°, lon=-81.0000° (central meridian, round number)
    //   Expected: easting ≈ false easting = 656167 ft (at lon=lon0)
    //             northing > 0 (north of 24°20' origin)
    private const double RefLat = 30.0;
    private const double RefLon = -81.0;  // exactly the central meridian

    [Fact]
    public void ToStatePlane_CentralMeridian_EastingNearFalseEasting()
    {
        // At the exact central meridian, easting = false easting = 200000m = ~656167 ft
        var (eFt, _) = StatePlaneProjection.ToStatePlane(RefLat, RefLon);
        const double fe = 200_000.0 * 3937.0 / 1200.0;   // 656166.67 ft
        Assert.InRange(eFt, fe - 5.0, fe + 5.0);
    }

    [Fact]
    public void ToStatePlane_CentralMeridian_NorthingIsPositive()
    {
        var (_, nFt) = StatePlaneProjection.ToStatePlane(RefLat, RefLon);
        Assert.True(nFt > 0, $"Expected northing > 0, got {nFt}");
    }

    [Fact]
    public void Roundtrip_SP_LatLon_SP_EastingCloseEnough()
    {
        // Forward then inverse must be idempotent within 0.01 ft
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(RefLat, RefLon);
        var (lat, lon) = StatePlaneProjection.ToLatLon(eFt, nFt);
        var (eFt2, _)  = StatePlaneProjection.ToStatePlane(lat, lon);
        Assert.InRange(eFt2, eFt - 0.01, eFt + 0.01);
    }

    [Fact]
    public void Roundtrip_SP_LatLon_SP_NorthingCloseEnough()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(RefLat, RefLon);
        var (lat, lon) = StatePlaneProjection.ToLatLon(eFt, nFt);
        var (_, nFt2)  = StatePlaneProjection.ToStatePlane(lat, lon);
        Assert.InRange(nFt2, nFt - 0.01, nFt + 0.01);
    }

    [Fact]
    public void Roundtrip_LatLon_SP_LatLon_LatCloseEnough()
    {
        // Inverse then forward must be idempotent within ~1e-8 degrees (<0.001 ft on the ground)
        var (eFt, nFt)   = StatePlaneProjection.ToStatePlane(RefLat, RefLon);
        var (lat2, lon2) = StatePlaneProjection.ToLatLon(eFt, nFt);
        Assert.InRange(lat2, RefLat - 1e-7, RefLat + 1e-7);
    }

    [Fact]
    public void Roundtrip_LatLon_SP_LatLon_LonCloseEnough()
    {
        var (eFt, nFt)   = StatePlaneProjection.ToStatePlane(RefLat, RefLon);
        var (lat2, lon2) = StatePlaneProjection.ToLatLon(eFt, nFt);
        Assert.InRange(lon2, RefLon - 1e-7, RefLon + 1e-7);
    }

    [Fact]
    public void ToLatLon_OutputLatitudeInFloridaRange()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(30.5, -81.5);
        var (lat, _) = StatePlaneProjection.ToLatLon(eFt, nFt);
        Assert.InRange(lat, 24.0, 31.5);
    }

    [Fact]
    public void ToLatLon_OutputLongitudeInFloridaRange()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(30.5, -81.5);
        var (_, lon) = StatePlaneProjection.ToLatLon(eFt, nFt);
        Assert.InRange(lon, -83.0, -79.5);
    }

    // ── ToDms ────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDms_PositiveLatitude_ContainsNSuffix()
    {
        var s = StatePlaneProjection.ToDms(30.3186, isLatitude: true);
        Assert.EndsWith("N", s);
    }

    [Fact]
    public void ToDms_NegativeLongitude_ContainsWSuffix()
    {
        var s = StatePlaneProjection.ToDms(-81.6648, isLatitude: false);
        Assert.EndsWith("W", s);
    }

    [Fact]
    public void ToDms_PositiveLongitude_ContainsESuffix()
    {
        var s = StatePlaneProjection.ToDms(10.0, isLatitude: false);
        Assert.EndsWith("E", s);
    }

    [Fact]
    public void ToDms_NegativeLatitude_ContainsSSuffix()
    {
        var s = StatePlaneProjection.ToDms(-5.0, isLatitude: true);
        Assert.EndsWith("S", s);
    }

    [Fact]
    public void ToDms_ContainsDegreeSymbol()
    {
        var s = StatePlaneProjection.ToDms(30.5, isLatitude: true);
        Assert.Contains("\u00b0", s);
    }

    [Fact]
    public void ToDms_ContainsMinuteMark()
    {
        var s = StatePlaneProjection.ToDms(30.5, isLatitude: true);
        Assert.Contains("'", s);
    }

    // ── ParseLatLon ──────────────────────────────────────────────────────────

    [Fact]
    public void ParseLatLon_DecimalDegrees_ReturnsValue()
    {
        double v = StatePlaneProjection.ParseLatLon("30.3186");
        Assert.InRange(v, 30.3186 - 1e-6, 30.3186 + 1e-6);
    }

    [Fact]
    public void ParseLatLon_SignedNegativeDecimal_ReturnsNegative()
    {
        double v = StatePlaneProjection.ParseLatLon("-81.6648");
        Assert.InRange(v, -81.6648 - 1e-6, -81.6648 + 1e-6);
    }

    [Fact]
    public void ParseLatLon_DmsWithNSuffix_Positive()
    {
        // 30°19'07.2\"N ≈ 30.31867°
        double v = StatePlaneProjection.ParseLatLon("30\u00b019'07.2\"N");
        Assert.InRange(v, 30.318, 30.320);
    }

    [Fact]
    public void ParseLatLon_DmsWithWSuffix_Negative()
    {
        // 81°39'53.3\"W ≈ -81.6648°
        double v = StatePlaneProjection.ParseLatLon("81\u00b039'53.3\"W");
        Assert.InRange(v, -81.666, -81.664);
    }

    [Fact]
    public void ParseLatLon_EmptyString_ReturnsNaN()
    {
        Assert.True(double.IsNaN(StatePlaneProjection.ParseLatLon("")));
    }

    [Fact]
    public void ParseLatLon_Garbage_ReturnsNaN()
    {
        Assert.True(double.IsNaN(StatePlaneProjection.ParseLatLon("NOT_A_NUMBER")));
    }

    [Fact]
    public void ParseLatLon_InvariantDecimalSeparator()
    {
        // Should parse period as decimal regardless of locale
        double v = StatePlaneProjection.ParseLatLon("30.5");
        Assert.InRange(v, 30.49, 30.51);
    }

    [Fact]
    public void ToDms_Roundtrip_ParseRoundTrip()
    {
        // ToDms then ParseLatLon must recover approximately the same value
        double original = 30.3322;
        string dms      = StatePlaneProjection.ToDms(original, isLatitude: true);
        double parsed   = StatePlaneProjection.ParseLatLon(dms);
        Assert.InRange(parsed, original - 0.0001, original + 0.0001);
    }
}
