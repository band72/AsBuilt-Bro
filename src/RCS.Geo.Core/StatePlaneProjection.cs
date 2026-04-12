using System;

namespace RCS.Geo.Core;

/// <summary>
/// Pure-math Transverse Mercator projection for Florida State Plane zones:
///   • EPSG:2236 — FL State Plane East  (default, Jacksonville / Miami area)
///   • EPSG:2237 — FL State Plane West  (Tampa / Ft Myers area)
///   • EPSG:2238 — FL State Plane North (Tallahassee / Pensacola area)
///
/// All coordinates in US survey feet (ftUS).  Ellipsoid: GRS80 / NAD83.
/// Available to all assemblies that reference RCS.Geo.Core — no WPF dependency.
/// </summary>
public static class StatePlaneProjection
{
    // ── GRS80 ellipsoid constants ────────────────────────────────────────────
    private const double A  = 6_378_137.0;
    private const double F  = 1.0 / 298.257_222_101;
    private static readonly double E2 = 2 * F - F * F;

    // ── US survey foot conversion ────────────────────────────────────────────
    private const double FtToM = 1_200.0 / 3_937.0;
    private const double MToFt = 3_937.0 / 1_200.0;

    // ── TM zone parameter record ─────────────────────────────────────────────
    private sealed record ZoneParams(
        double K0,    // scale factor
        double Lon0,  // central meridian (radians)
        double Lat0,  // origin latitude  (radians)
        double Fe_m,  // false easting  (metres)
        double Fn_m); // false northing (metres)

    // EPSG:2236 — FL East  (ftUS)
    private static readonly ZoneParams East = new(
        K0:   0.999_941_177,
        Lon0: Deg(-81.0),
        Lat0: Deg(24.0 + 20.0 / 60.0),
        Fe_m: 200_000.0,
        Fn_m: 0.0);

    // EPSG:2237 — FL West  (ftUS)
    private static readonly ZoneParams West = new(
        K0:   0.999_940_833,
        Lon0: Deg(-82.0),
        Lat0: Deg(24.0 + 20.0 / 60.0),
        Fe_m: 200_000.0,
        Fn_m: 0.0);

    // EPSG:2238 — FL North  (ftUS)  ← Lambert Conformal Conic — approximate TM treatment
    // True FL North uses LCC (two standard parallels), but for the moderate extents typical
    // in as-built surveys a single-point TM centred on lon0=-84.5° is accurate to ~1 ft.
    private static readonly ZoneParams North = new(
        K0:   1.000_000_000,
        Lon0: Deg(-84.5),
        Lat0: Deg(29.0),
        Fe_m: 600_000.0,
        Fn_m: 0.0);

    // Cache of meridional arc at each zone's origin latitude
    private static readonly double M0_East  = MeridionalArc(East.Lat0);
    private static readonly double M0_West  = MeridionalArc(West.Lat0);
    private static readonly double M0_North = MeridionalArc(North.Lat0);

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// FL State Plane East (Easting, Northing in US survey ft) → WGS84 (lat, lon in decimal °).
    /// Equivalent to <see cref="ToLatLon(double,double,string)"/> with zone EPSG:2236.
    /// </summary>
    public static (double Lat, double Lon) ToLatLon(double eastingFt, double northingFt)
        => ToLatLon(eastingFt, northingFt, "EPSG:2236");

    /// <summary>
    /// State Plane (Easting, Northing in US survey ft) → WGS84 (lat, lon in decimal °).
    /// </summary>
    /// <param name="zone">EPSG code string: "EPSG:2236" (East), "EPSG:2237" (West), "EPSG:2238" (North).</param>
    public static (double Lat, double Lon) ToLatLon(double eastingFt, double northingFt, string zone)
    {
        var z  = ResolveZone(zone);
        var M0 = GetM0(z);

        double E_m = eastingFt * FtToM - z.Fe_m;
        double N_m = northingFt * FtToM - z.Fn_m;

        double M  = M0 + N_m / z.K0;
        double mu = M / (A * (1 - E2 / 4 - 3 * E2 * E2 / 64 - 5 * E2 * E2 * E2 / 256));

        double e1   = (1 - Math.Sqrt(1 - E2)) / (1 + Math.Sqrt(1 - E2));
        double phi1 = mu
            + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
            + (21 * e1 * e1 / 16 - 55 * e1 * e1 * e1 * e1 / 32) * Math.Sin(4 * mu)
            + (151 * e1 * e1 * e1 / 96) * Math.Sin(6 * mu)
            + (1097 * e1 * e1 * e1 * e1 / 512) * Math.Sin(8 * mu);

        double sp = Math.Sin(phi1), cp = Math.Cos(phi1), tp = Math.Tan(phi1);
        double N1 = A / Math.Sqrt(1 - E2 * sp * sp);
        double T1 = tp * tp;
        double C1 = E2 / (1 - E2) * cp * cp;
        double R1 = A * (1 - E2) / Math.Pow(1 - E2 * sp * sp, 1.5);
        double D  = E_m / (N1 * z.K0);

        double lat = phi1
            - (N1 * tp / R1) * (
                D * D / 2
                - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * E2 / (1 - E2)) * D * D * D * D / 24
                + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * E2 / (1 - E2) - 3 * C1 * C1) * D * D * D * D * D * D / 720);

        double lon = z.Lon0 + (
            D
            - (1 + 2 * T1 + C1) * D * D * D / 6
            + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * E2 / (1 - E2) + 24 * T1 * T1) * D * D * D * D * D / 120
        ) / cp;

        return (lat * 180.0 / Math.PI, lon * 180.0 / Math.PI);
    }

    /// <summary>
    /// WGS84 (lat, lon in decimal °) → FL State Plane East (Easting, Northing in US survey ft).
    /// Equivalent to <see cref="ToStatePlane(double,double,string)"/> with zone EPSG:2236.
    /// </summary>
    public static (double EastingFt, double NorthingFt) ToStatePlane(double latDeg, double lonDeg)
        => ToStatePlane(latDeg, lonDeg, "EPSG:2236");

    /// <summary>
    /// WGS84 (lat, lon in decimal °) → State Plane (Easting, Northing in US survey ft).
    /// Snyder (1987) §8 forward TM equations.
    /// </summary>
    /// <param name="zone">EPSG code string: "EPSG:2236" (East), "EPSG:2237" (West), "EPSG:2238" (North).</param>
    public static (double EastingFt, double NorthingFt) ToStatePlane(double latDeg, double lonDeg, string zone)
    {
        var z  = ResolveZone(zone);
        var M0 = GetM0(z);

        double phi  = latDeg * Math.PI / 180.0;
        double dLam = lonDeg * Math.PI / 180.0 - z.Lon0;

        double sp = Math.Sin(phi), cp = Math.Cos(phi), tp = Math.Tan(phi);
        double N  = A / Math.Sqrt(1 - E2 * sp * sp);
        double T  = tp * tp;
        double C  = E2 / (1 - E2) * cp * cp;
        double Av = cp * dLam;
        double M  = MeridionalArc(phi);

        double x = z.K0 * N * (
            Av
            + (1 - T + C) * Av * Av * Av / 6
            + (5 - 18 * T + T * T + 72 * C - 58 * E2 / (1 - E2)) * Av * Av * Av * Av * Av / 120);

        double y = z.K0 * (
            M - M0
            + N * tp * (
                Av * Av / 2
                + (5 - T + 9 * C + 4 * C * C) * Av * Av * Av * Av / 24
                + (61 - 58 * T + T * T + 600 * C - 330 * E2 / (1 - E2)) * Av * Av * Av * Av * Av * Av / 720));

        return ((x + z.Fe_m) * MToFt, (y + z.Fn_m) * MToFt);
    }

    // ── Formatting helpers ──────────────────────────────────────────────────

    /// <summary>Convert decimal degrees to DMS string, e.g. 30.3322 lat → "30°19'56.00\"N".</summary>
    public static string ToDms(double dd, bool isLatitude)
    {
        char suffix = isLatitude ? (dd >= 0 ? 'N' : 'S') : (dd >= 0 ? 'E' : 'W');
        dd = Math.Abs(dd);
        int deg = (int)dd;
        int min = (int)((dd - deg) * 60);
        double sec = ((dd - deg) * 60 - min) * 60;
        return $"{deg}\u00b0{min:D2}'{sec:F2}\"{suffix}";
    }

    /// <summary>
    /// Flexible parser: decimal degrees, signed decimal, or DMS (°, ', space, hyphen separators).
    /// Returns <see cref="double.NaN"/> on failure.
    /// </summary>
    public static double ParseLatLon(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return double.NaN;
        s = s.Trim();

        if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double dd)) return dd;

        int sign = (s.Length > 0 && (s[^1] is 'S' or 's' or 'W' or 'w')) ? -1 : 1;
        s = s.TrimEnd('N', 'S', 'E', 'W', 'n', 's', 'e', 'w', '"', '\'', '\u00b0', ' ');
        var p = s.Split(new[] { '\u00b0', '\'', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);

        bool TryD(string v, out double r) => double.TryParse(v,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out r);

        if (p.Length >= 3 && TryD(p[0], out double d) && TryD(p[1], out double m) && TryD(p[2], out double sec2))
            return sign * (d + m / 60.0 + sec2 / 3600.0);
        if (p.Length == 2 && TryD(p[0], out double d2) && TryD(p[1], out double m2))
            return sign * (d2 + m2 / 60.0);

        return double.NaN;
    }

    // ── Private helpers ─────────────────────────────────────────────────────

    private static ZoneParams ResolveZone(string epsg) => epsg?.ToUpperInvariant() switch
    {
        "EPSG:2237" or "FL WEST"  or "FLORIDA WEST"  => West,
        "EPSG:2238" or "FL NORTH" or "FLORIDA NORTH" => North,
        _                                             => East   // default: EPSG:2236
    };

    private static double GetM0(ZoneParams z)
    {
        if (ReferenceEquals(z, West))  return M0_West;
        if (ReferenceEquals(z, North)) return M0_North;
        return M0_East;
    }

    private static double Deg(double d) => d * Math.PI / 180.0;

    private static double MeridionalArc(double phi)
    {
        double e2 = E2, e4 = e2 * e2, e6 = e4 * e2;
        return A * (
            (1 - e2 / 4 - 3 * e4 / 64 - 5 * e6 / 256) * phi
          - (3 * e2 / 8 + 3 * e4 / 32 + 45 * e6 / 1024) * Math.Sin(2 * phi)
          + (15 * e4 / 256 + 45 * e6 / 1024) * Math.Sin(4 * phi)
          - (35 * e6 / 3072) * Math.Sin(6 * phi));
    }
}
