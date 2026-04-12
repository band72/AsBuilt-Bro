using RCS.Geo.Core;

namespace RCS.Cogo.Wpf.Services;

/// <summary>
/// WPF-layer facade over <see cref="StatePlaneProjection"/> (RCS.Geo.Core).
/// All projection math lives in the shared library; this wrapper is kept for
/// backward-compatibility with call sites in RCS.Cogo.Wpf (ShellViewModel, etc.)
/// </summary>
public static class StatePlaneConverter
{
    /// <inheritdoc cref="StatePlaneProjection.ToLatLon"/>
    public static (double Lat, double Lon) ToLatLon(double eastingFt, double northingFt)
        => StatePlaneProjection.ToLatLon(eastingFt, northingFt);

    /// <inheritdoc cref="StatePlaneProjection.ToStatePlane"/>
    public static (double EastingFt, double NorthingFt) ToStatePlane(double latDeg, double lonDeg)
        => StatePlaneProjection.ToStatePlane(latDeg, lonDeg);

    /// <inheritdoc cref="StatePlaneProjection.ToDms"/>
    public static string ToDms(double dd, bool isLatitude)
        => StatePlaneProjection.ToDms(dd, isLatitude);

    /// <inheritdoc cref="StatePlaneProjection.ParseLatLon"/>
    public static double ParseLatLon(string s)
        => StatePlaneProjection.ParseLatLon(s);

    /// <summary>Returns true if coordinates are within the JEA / NE-Florida service area.</summary>
    public static bool IsInJeaBounds(double eastingFt, double northingFt)
        => eastingFt is > 320_000 and < 590_000 && northingFt is > 1_920_000 and < 2_370_000;

    public static bool IsLatLonInJeaBounds(double lat, double lon)
        => lat is > 29.0 and < 31.0 && lon is > -83.0 and < -80.0;
}
