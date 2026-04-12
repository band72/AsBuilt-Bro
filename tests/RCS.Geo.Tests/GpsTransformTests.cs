using System;
using System.IO;
using System.Text;
using RCS.Geo.Core;

namespace RCS.Geo.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// StatePlaneProjectionTests — forward (SP→LatLon) and inverse (LatLon→SP)
//   Covers FL East (TM EPSG:2236), FL West (TM EPSG:2237),
//          FL North (LCC EPSG:2238), and NormalizeZone helper
// ─────────────────────────────────────────────────────────────────────────────
public class StatePlaneProjectionTests
{
    // ── FL East (EPSG:2236) — TM ─────────────────────────────────────────────

    private const double RefLat = 30.0;
    private const double RefLon = -81.0;   // exactly the FL East central meridian

    [Fact]
    public void ToStatePlane_CentralMeridian_EastingNearFalseEasting()
    {
        var (eFt, _) = StatePlaneProjection.ToStatePlane(RefLat, RefLon);
        const double fe = 200_000.0 * 3937.0 / 1200.0;   // ≈ 656166.67 ft
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

    // ── FL West (EPSG:2237) — TM ─────────────────────────────────────────────
    // Central meridian: -82°, false easting 200 000 m, Tampa / Fort Myers area

    private const double WestLat = 27.5;   // Tampa latitude
    private const double WestLon = -82.0;  // exactly FL West central meridian

    [Fact]
    public void FLWest_ForwardAtCentralMeridian_EastingNearFalseEasting()
    {
        var (eFt, _) = StatePlaneProjection.ToStatePlane(WestLat, WestLon, "EPSG:2237");
        const double fe = 200_000.0 * 3937.0 / 1200.0;
        Assert.InRange(eFt, fe - 5.0, fe + 5.0);
    }

    [Fact]
    public void FLWest_Roundtrip_LatCloseEnough()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(WestLat, WestLon, "EPSG:2237");
        var (lat2, _)  = StatePlaneProjection.ToLatLon(eFt, nFt, "EPSG:2237");
        Assert.InRange(lat2, WestLat - 1e-7, WestLat + 1e-7);
    }

    [Fact]
    public void FLWest_Roundtrip_LonCloseEnough()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(WestLat, WestLon, "EPSG:2237");
        var (_, lon2)  = StatePlaneProjection.ToLatLon(eFt, nFt, "EPSG:2237");
        Assert.InRange(lon2, WestLon - 1e-7, WestLon + 1e-7);
    }

    [Fact]
    public void FLWest_ZoneString_Alias_Works()
    {
        // "FL West" alias should resolve the same as EPSG:2237
        var (e1, n1) = StatePlaneProjection.ToStatePlane(WestLat, WestLon, "EPSG:2237");
        var (e2, n2) = StatePlaneProjection.ToStatePlane(WestLat, WestLon, "FL West");
        Assert.InRange(e2, e1 - 0.001, e1 + 0.001);
        Assert.InRange(n2, n1 - 0.001, n1 + 0.001);
    }

    [Fact]
    public void FLWest_Offsets_Meaningfully_Different_From_FLEast()
    {
        // Same geodetic point should yield a different easting in FL West vs FL East
        var (eW, _) = StatePlaneProjection.ToStatePlane(28.0, -82.5, "EPSG:2237");
        var (eE, _) = StatePlaneProjection.ToStatePlane(28.0, -82.5, "EPSG:2236");
        Assert.NotEqual(Math.Round(eW, 0), Math.Round(eE, 0));
    }

    // ── FL North (EPSG:2238) — Lambert Conformal Conic ───────────────────────
    // Standard parallels: 29°34' N / 30°45' N    CM: 84°30' W    FE: 600 000 m
    // Test area: Tallahassee / Pensacola region

    private const double NorthLat = 30.0;   // typical N FL latitude
    private const double NorthLon = -84.5;  // exactly the LCC central meridian

    [Fact]
    public void FLNorth_ForwardAtCentralMeridian_EastingNearFalseEasting()
    {
        // At the central meridian, sin(theta)=0 → x=0 → easting ≈ false easting
        var (eFt, _) = StatePlaneProjection.ToStatePlane(NorthLat, NorthLon, "EPSG:2238");
        const double fe = 600_000.0 * 3937.0 / 1200.0;   // ≈ 1 968 500 ft
        Assert.InRange(eFt, fe - 5.0, fe + 5.0);
    }

    [Fact]
    public void FLNorth_Roundtrip_LatCloseEnough()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(NorthLat, NorthLon, "EPSG:2238");
        var (lat2, _)  = StatePlaneProjection.ToLatLon(eFt, nFt, "EPSG:2238");
        Assert.InRange(lat2, NorthLat - 1e-7, NorthLat + 1e-7);
    }

    [Fact]
    public void FLNorth_Roundtrip_LonCloseEnough()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(NorthLat, NorthLon, "EPSG:2238");
        var (_, lon2)  = StatePlaneProjection.ToLatLon(eFt, nFt, "EPSG:2238");
        Assert.InRange(lon2, NorthLon - 1e-7, NorthLon + 1e-7);
    }

    [Fact]
    public void FLNorth_Pensacola_Roundtrip_Lat()
    {
        // Pensacola: lat≈30.42°, lon≈-87.21°  (western edge of FL North zone)
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(30.42, -87.21, "EPSG:2238");
        var (lat2, _)  = StatePlaneProjection.ToLatLon(eFt, nFt, "EPSG:2238");
        Assert.InRange(lat2, 30.42 - 1e-5, 30.42 + 1e-5);
    }

    [Fact]
    public void FLNorth_Pensacola_Roundtrip_Lon()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(30.42, -87.21, "EPSG:2238");
        var (_, lon2)  = StatePlaneProjection.ToLatLon(eFt, nFt, "EPSG:2238");
        Assert.InRange(lon2, -87.21 - 1e-5, -87.21 + 1e-5);
    }

    [Fact]
    public void FLNorth_Tallahassee_Roundtrip_Lat()
    {
        // Tallahassee: lat≈30.44°, lon≈-84.28°
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(30.44, -84.28, "EPSG:2238");
        var (lat2, _)  = StatePlaneProjection.ToLatLon(eFt, nFt, "EPSG:2238");
        Assert.InRange(lat2, 30.44 - 1e-7, 30.44 + 1e-7);
    }

    [Fact]
    public void FLNorth_Tallahassee_Roundtrip_Lon()
    {
        var (eFt, nFt) = StatePlaneProjection.ToStatePlane(30.44, -84.28, "EPSG:2238");
        var (_, lon2)  = StatePlaneProjection.ToLatLon(eFt, nFt, "EPSG:2238");
        Assert.InRange(lon2, -84.28 - 1e-7, -84.28 + 1e-7);
    }

    [Fact]
    public void FLNorth_Alias_Works()
    {
        var (e1, n1) = StatePlaneProjection.ToStatePlane(NorthLat, NorthLon, "EPSG:2238");
        var (e2, n2) = StatePlaneProjection.ToStatePlane(NorthLat, NorthLon, "FL NORTH");
        Assert.InRange(e2, e1 - 0.001, e1 + 0.001);
        Assert.InRange(n2, n1 - 0.001, n1 + 0.001);
    }

    [Fact]
    public void FLNorth_Offsets_Meaningfully_Different_From_FLEast()
    {
        // Same geodetic point should yield a different easting
        var (eN, _) = StatePlaneProjection.ToStatePlane(30.5, -84.5, "EPSG:2238");
        var (eE, _) = StatePlaneProjection.ToStatePlane(30.5, -84.5, "EPSG:2236");
        Assert.NotEqual(Math.Round(eN, 0), Math.Round(eE, 0));
    }

    // ── NormalizeZone ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("EPSG:2236",               "EPSG:2236")]
    [InlineData("epsg:2236",               "EPSG:2236")]
    [InlineData("Florida East (EPSG:2236)","EPSG:2236")]
    [InlineData("FL EAST",                 "EPSG:2236")]
    [InlineData(null,                      "EPSG:2236")]
    [InlineData("",                        "EPSG:2236")]
    [InlineData("garbage",                 "EPSG:2236")]
    public void NormalizeZone_EastVariants_ReturnsEPSG2236(string? input, string expected)
        => Assert.Equal(expected, StatePlaneProjection.NormalizeZone(input));

    [Theory]
    [InlineData("EPSG:2237",               "EPSG:2237")]
    [InlineData("epsg:2237",               "EPSG:2237")]
    [InlineData("Florida West (EPSG:2237)","EPSG:2237")]
    [InlineData("FL West",                 "EPSG:2237")]
    [InlineData("WEST",                    "EPSG:2237")]
    public void NormalizeZone_WestVariants_ReturnsEPSG2237(string input, string expected)
        => Assert.Equal(expected, StatePlaneProjection.NormalizeZone(input));

    [Theory]
    [InlineData("EPSG:2238",                "EPSG:2238")]
    [InlineData("epsg:2238",                "EPSG:2238")]
    [InlineData("Florida North (EPSG:2238)","EPSG:2238")]
    [InlineData("FL North",                 "EPSG:2238")]
    [InlineData("NORTH",                    "EPSG:2238")]
    public void NormalizeZone_NorthVariants_ReturnsEPSG2238(string input, string expected)
        => Assert.Equal(expected, StatePlaneProjection.NormalizeZone(input));

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
        double v = StatePlaneProjection.ParseLatLon("30\u00b019'07.2\"N");
        Assert.InRange(v, 30.318, 30.320);
    }

    [Fact]
    public void ParseLatLon_DmsWithWSuffix_Negative()
    {
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
        double v = StatePlaneProjection.ParseLatLon("30.5");
        Assert.InRange(v, 30.49, 30.51);
    }

    [Fact]
    public void ToDms_Roundtrip_ParseRoundTrip()
    {
        double original = 30.3322;
        string dms      = StatePlaneProjection.ToDms(original, isLatitude: true);
        double parsed   = StatePlaneProjection.ParseLatLon(dms);
        Assert.InRange(parsed, original - 0.0001, original + 0.0001);
    }
}
