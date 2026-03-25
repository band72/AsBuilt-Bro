using System;

namespace RCS.Cogo.Wpf.Services;

/// <summary>
/// Converts between Florida State Plane East (EPSG:2236, NAD83, US Survey Feet)
/// and WGS84 Geographic coordinates (decimal degrees).
///
/// Projection parameters (EPSG:2236 – NAD83 / Florida East (ftUS)):
///   Transverse Mercator
///   Latitude of origin  : 24°20'00" N  = 24.33333...°
///   Central meridian    : 81°00'00" W  = -81.0°
///   Scale factor k₀     : 0.999941177
///   False easting       : 200000 m     = 656166.6667 US survey ft
///   False northing      : 0 m
///   Ellipsoid           : GRS80 (= WGS84 for our purposes)
/// </summary>
public static class StatePlaneConverter
{
    // GRS80 / WGS84 ellipsoid
    private const double A  = 6378137.0;              // semi-major axis (m)
    private const double F  = 1.0 / 298.257222101;    // flattening
    private static readonly double B  = A * (1 - F);  // semi-minor axis
    private static readonly double E2 = 2 * F - F * F; // eccentricity²
    private static readonly double E  = Math.Sqrt(E2);

    // EPSG:2236 projection constants
    private const double K0     = 0.999941177;
    private const double Lon0   = -81.0 * Math.PI / 180.0;   // central meridian (rad)
    private const double Lat0   = (24.0 + 20.0 / 60.0) * Math.PI / 180.0; // origin lat (rad)
    private const double Fe_m   = 200000.0;                   // false easting (meters)
    private const double Fn_m   = 0.0;                        // false northing (meters)

    // 1 US survey foot = exactly 1200/3937 meters
    private const double FtToM  = 1200.0 / 3937.0;
    private const double MToFt  = 3937.0 / 1200.0;

    // M₀ — meridional arc at latitude of origin
    private static readonly double M0 = MeridionalArc(Lat0);

    /// <summary>
    /// Convert Florida State Plane East (Easting, Northing in US survey feet)
    /// to WGS84 (latitude, longitude in decimal degrees).
    /// </summary>
    public static (double Lat, double Lon) ToLatLon(double eastingFt, double northingFt)
    {
        // Convert to meters
        double E_m = eastingFt * FtToM - Fe_m;   // offset from central meridian false easting
        double N_m = northingFt * FtToM - Fn_m;

        // TM inverse
        double M  = M0 + N_m / K0;
        double mu = M / (A * (1 - E2 / 4 - 3 * E2 * E2 / 64 - 5 * E2 * E2 * E2 / 256));

        double e1 = (1 - Math.Sqrt(1 - E2)) / (1 + Math.Sqrt(1 - E2));

        double phi1 = mu
            + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
            + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
            + (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu)
            + (1097 * e1 * e1 * e1 * e1 / 512) * Math.Sin(8 * mu);

        double sinPhi1 = Math.Sin(phi1);
        double cosPhi1 = Math.Cos(phi1);
        double tanPhi1 = Math.Tan(phi1);

        double N1  = A / Math.Sqrt(1 - E2 * sinPhi1 * sinPhi1);
        double T1  = tanPhi1 * tanPhi1;
        double C1  = E2 / (1 - E2) * cosPhi1 * cosPhi1;
        double R1  = A * (1 - E2) / Math.Pow(1 - E2 * sinPhi1 * sinPhi1, 1.5);
        double D   = E_m / (N1 * K0);

        double lat = phi1
            - (N1 * tanPhi1 / R1) * (
                D * D / 2
                - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * E2 / (1 - E2)) * D * D * D * D / 24
                + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * E2 / (1 - E2) - 3 * C1 * C1) * D * D * D * D * D * D / 720
            );

        double lon = Lon0 + (
            D
            - (1 + 2 * T1 + C1) * D * D * D / 6
            + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * E2 / (1 - E2) + 24 * T1 * T1) * D * D * D * D * D / 120
        ) / cosPhi1;

        return (lat * 180.0 / Math.PI, lon * 180.0 / Math.PI);
    }

    /// <summary>
    /// Quick sanity check — returns true if the coordinates are plausibly within
    /// the Jacksonville/JEA service area bounds from the JEA Validation Rules sheet.
    /// </summary>
    public static bool IsInJeaBounds(double eastingFt, double northingFt)
        => eastingFt is > 320_000 and < 590_000 &&
           northingFt is > 1_920_000 and < 2_370_000;

    public static bool IsLatLonInJeaBounds(double lat, double lon)
        => lat is > 29.0 and < 31.0 && lon is > -83.0 and < -80.0;

    /// <summary>Meridional arc length from equator to latitude φ (radians).</summary>
    private static double MeridionalArc(double phi)
    {
        double e2 = E2, e4 = e2 * e2, e6 = e4 * e2;
        return A * (
            (1 - e2 / 4 - 3 * e4 / 64   - 5 * e6 / 256)   * phi
          - (3 * e2 / 8 + 3 * e4 / 32   + 45 * e6 / 1024)  * Math.Sin(2 * phi)
          + (15 * e4 / 256 + 45 * e6 / 1024)                * Math.Sin(4 * phi)
          - (35 * e6 / 3072)                                 * Math.Sin(6 * phi)
        );
    }
}
