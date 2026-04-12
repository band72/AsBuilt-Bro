using System;

namespace RCS.Geo.Core;

/// <summary>
/// Pure-math projection for Florida State Plane zones:
///   • EPSG:2236 — FL East  (Transverse Mercator, Jacksonville / Miami)
///   • EPSG:2237 — FL West  (Transverse Mercator, Tampa / Fort Myers)
///   • EPSG:2238 — FL North (Lambert Conformal Conic, Tallahassee / Pensacola)
///
/// All coordinates in US survey feet (ftUS).  Ellipsoid: GRS80 / NAD83.
/// Available to all assemblies that reference RCS.Geo.Core — no WPF dependency.
/// </summary>
public static class StatePlaneProjection
{
    // ── GRS80 ellipsoid ─────────────────────────────────────────────────────
    private const double A   = 6_378_137.0;
    private const double F   = 1.0 / 298.257_222_101;
    private static readonly double E2  = 2 * F - F * F;
    private static readonly double E   = Math.Sqrt(E2);

    // ── Unit conversion ──────────────────────────────────────────────────────
    private const double FtToM = 1_200.0 / 3_937.0;
    private const double MToFt = 3_937.0 / 1_200.0;

    // ════════════════════════════════════════════════════════════════════════
    //  TM zone parameters (FL East, FL West)
    // ════════════════════════════════════════════════════════════════════════

    private sealed record TmZone(double K0, double Lon0, double Lat0, double Fe_m, double Fn_m);

    // EPSG:2236 — FL East (central meridian 81° W)
    private static readonly TmZone TmEast = new(
        K0:   0.999_941_177,
        Lon0: Deg(-81.0),
        Lat0: Deg(24.0 + 20.0 / 60.0),
        Fe_m: 200_000.0, Fn_m: 0.0);

    // EPSG:2237 — FL West (central meridian 82° W)
    private static readonly TmZone TmWest = new(
        K0:   0.999_940_833,
        Lon0: Deg(-82.0),
        Lat0: Deg(24.0 + 20.0 / 60.0),
        Fe_m: 200_000.0, Fn_m: 0.0);

    private static readonly double M0_East = MeridionalArc(TmEast.Lat0);
    private static readonly double M0_West = MeridionalArc(TmWest.Lat0);

    // ════════════════════════════════════════════════════════════════════════
    //  LCC zone parameters (FL North) — Snyder (1987) §15
    //  EPSG:2238  NAD83 / Florida North  ftUS
    //  Standard parallels: 29° 34' N and 30° 45' N
    //  Origin latitude:    29° N
    //  Central meridian:   84° 30' W
    //  False Easting:      600 000 m  False Northing: 0 m
    // ════════════════════════════════════════════════════════════════════════

    private static readonly double LccPhi1 = Deg(29.0 + 34.0 / 60.0);   // sp1
    private static readonly double LccPhi2 = Deg(30.0 + 45.0 / 60.0);   // sp2
    private static readonly double LccPhi0 = Deg(29.0);                   // origin lat
    private const           double LccLon0 = -84.5 * Math.PI / 180.0;    // central meridian
    private const           double LccFe_m = 600_000.0;
    private const           double LccFn_m = 0.0;

    // Pre-compute LCC constants (Snyder §15, p.107)
    private static readonly double LccN, LccF, LccRho0;

    static StatePlaneProjection()
    {
        // m(phi) = cos(phi) / sqrt(1 - e²·sin²(phi))
        static double LccM(double phi)
        {
            double sp = Math.Sin(phi);
            return Math.Cos(phi) / Math.Sqrt(1 - E2 * sp * sp);
        }
        // t(phi) = tan(π/4 - phi/2) / [(1-e·sin(phi))/(1+e·sin(phi))]^(e/2)
        static double LccT(double phi)
        {
            double sp = E * Math.Sin(phi);
            return Math.Tan(Math.PI / 4 - phi / 2)
                   / Math.Pow((1 - sp) / (1 + sp), E / 2);
        }

        double m1 = LccM(LccPhi1), m2 = LccM(LccPhi2);
        double t0 = LccT(LccPhi0), t1 = LccT(LccPhi1), t2 = LccT(LccPhi2);

        LccN    = (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));
        LccF    = m1 / (LccN * Math.Pow(t1, LccN));
        LccRho0 = A * LccF * Math.Pow(t0, LccN);    // rho at origin latitude
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>FL State Plane East (ftUS) → WGS84 decimal degrees. (Default zone: EPSG:2236.)</summary>
    public static (double Lat, double Lon) ToLatLon(double eastingFt, double northingFt)
        => ToLatLon(eastingFt, northingFt, "EPSG:2236");

    /// <summary>
    /// State Plane (ftUS) → WGS84 decimal degrees.
    /// Zone: "EPSG:2236" East | "EPSG:2237" West | "EPSG:2238" North.
    /// Case-insensitive; aliases "FL EAST" / "FL WEST" / "FL NORTH" accepted.
    /// </summary>
    public static (double Lat, double Lon) ToLatLon(double eastingFt, double northingFt, string zone)
        => NormalizeZone(zone) switch
        {
            "EPSG:2238" => LccInverse(eastingFt, northingFt),
            "EPSG:2237" => TmInverse(eastingFt, northingFt, TmWest, M0_West),
            _           => TmInverse(eastingFt, northingFt, TmEast, M0_East)
        };

    /// <summary>WGS84 decimal degrees → FL State Plane East (ftUS). (Default zone: EPSG:2236.)</summary>
    public static (double EastingFt, double NorthingFt) ToStatePlane(double latDeg, double lonDeg)
        => ToStatePlane(latDeg, lonDeg, "EPSG:2236");

    /// <summary>
    /// WGS84 decimal degrees → State Plane (ftUS).
    /// Zone: "EPSG:2236" East | "EPSG:2237" West | "EPSG:2238" North.
    /// </summary>
    public static (double EastingFt, double NorthingFt) ToStatePlane(double latDeg, double lonDeg, string zone)
        => NormalizeZone(zone) switch
        {
            "EPSG:2238" => LccForward(latDeg, lonDeg),
            "EPSG:2237" => TmForward(latDeg, lonDeg, TmWest, M0_West),
            _           => TmForward(latDeg, lonDeg, TmEast, M0_East)
        };

    // ── Formatting helpers ──────────────────────────────────────────────────

    /// <summary>Decimal degrees → DMS string, e.g. 30.3322 lat → "30°19'56.00\"N".</summary>
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

    // ── Zone normalizer ─────────────────────────────────────────────────────

    /// <summary>
    /// Maps a zone string (EPSG code, label, or alias) to a canonical EPSG ID.
    /// Recognises the same display strings used in <c>AvailableStatePlaneZones</c>.
    /// </summary>
    public static string NormalizeZone(string? zone)
    {
        if (string.IsNullOrWhiteSpace(zone)) return "EPSG:2236";
        string up = zone.ToUpperInvariant();
        if (up.Contains("2237") || up.Contains("WEST"))  return "EPSG:2237";
        if (up.Contains("2238") || up.Contains("NORTH")) return "EPSG:2238";
        return "EPSG:2236";   // default East
    }

    // ════════════════════════════════════════════════════════════════════════
    //  TM (Transverse Mercator) — Snyder (1987) §8
    // ════════════════════════════════════════════════════════════════════════

    private static (double Lat, double Lon) TmInverse(double eastingFt, double northingFt, TmZone z, double M0)
    {
        double E_m = eastingFt * FtToM - z.Fe_m;
        double N_m = northingFt * FtToM - z.Fn_m;

        double M  = M0 + N_m / z.K0;
        double mu = M / (A * (1 - E2 / 4 - 3 * E2 * E2 / 64 - 5 * E2 * E2 * E2 / 256));

        double e1   = (1 - Math.Sqrt(1 - E2)) / (1 + Math.Sqrt(1 - E2));
        double phi1 = mu
            + (3 * e1 / 2 - 27 * e1 * e1 * e1 / 32) * Math.Sin(2 * mu)
            + (21 * e1 * e1 / 16 - 55 * Math.Pow(e1, 4) / 32) * Math.Sin(4 * mu)
            + (151 * Math.Pow(e1, 3) / 96) * Math.Sin(6 * mu)
            + (1097 * Math.Pow(e1, 4) / 512) * Math.Sin(8 * mu);

        double sp = Math.Sin(phi1), cp = Math.Cos(phi1), tp = Math.Tan(phi1);
        double N1 = A / Math.Sqrt(1 - E2 * sp * sp);
        double T1 = tp * tp;
        double C1 = E2 / (1 - E2) * cp * cp;
        double R1 = A * (1 - E2) / Math.Pow(1 - E2 * sp * sp, 1.5);
        double D  = E_m / (N1 * z.K0);

        double lat = phi1
            - (N1 * tp / R1) * (
                D * D / 2
                - (5 + 3 * T1 + 10 * C1 - 4 * C1 * C1 - 9 * E2 / (1 - E2)) * Math.Pow(D, 4) / 24
                + (61 + 90 * T1 + 298 * C1 + 45 * T1 * T1 - 252 * E2 / (1 - E2) - 3 * C1 * C1) * Math.Pow(D, 6) / 720);

        double lon = z.Lon0 + (
            D
            - (1 + 2 * T1 + C1) * Math.Pow(D, 3) / 6
            + (5 - 2 * C1 + 28 * T1 - 3 * C1 * C1 + 8 * E2 / (1 - E2) + 24 * T1 * T1) * Math.Pow(D, 5) / 120
        ) / cp;

        return (lat * 180.0 / Math.PI, lon * 180.0 / Math.PI);
    }

    private static (double EastingFt, double NorthingFt) TmForward(double latDeg, double lonDeg, TmZone z, double M0)
    {
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
            + (1 - T + C) * Math.Pow(Av, 3) / 6
            + (5 - 18 * T + T * T + 72 * C - 58 * E2 / (1 - E2)) * Math.Pow(Av, 5) / 120);

        double y = z.K0 * (
            M - M0
            + N * tp * (
                Av * Av / 2
                + (5 - T + 9 * C + 4 * C * C) * Math.Pow(Av, 4) / 24
                + (61 - 58 * T + T * T + 600 * C - 330 * E2 / (1 - E2)) * Math.Pow(Av, 6) / 720));

        return ((x + z.Fe_m) * MToFt, (y + z.Fn_m) * MToFt);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  LCC (Lambert Conformal Conic) — Snyder (1987) §15
    //  Used for FL North (EPSG:2238) with standard parallels 29°34' / 30°45'
    // ════════════════════════════════════════════════════════════════════════

    private static (double Lat, double Lon) LccInverse(double eastingFt, double northingFt)
    {
        double x = eastingFt  * FtToM - LccFe_m;
        double y = northingFt * FtToM - LccFn_m;

        // rho' = sign(n) * sqrt(x² + (ρ₀ - y)²)
        double yDiff = LccRho0 - y;
        double rho   = Math.Sign(LccN) * Math.Sqrt(x * x + yDiff * yDiff);
        double theta = Math.Atan2(Math.Sign(LccN) * x, Math.Sign(LccN) * yDiff);

        double t = Math.Pow(rho / (A * LccF), 1.0 / LccN);

        // Iterative solution for phi (latitude) from t
        double phi = Math.PI / 2 - 2 * Math.Atan(t);
        for (int i = 0; i < 10; i++)
        {
            double sp = E * Math.Sin(phi);
            phi = Math.PI / 2 - 2 * Math.Atan(t * Math.Pow((1 - sp) / (1 + sp), E / 2));
        }

        double lon = theta / LccN + LccLon0;
        return (phi * 180.0 / Math.PI, lon * 180.0 / Math.PI);
    }

    private static (double EastingFt, double NorthingFt) LccForward(double latDeg, double lonDeg)
    {
        double phi = latDeg * Math.PI / 180.0;
        double lam = lonDeg * Math.PI / 180.0;

        double sp    = E * Math.Sin(phi);
        double t     = Math.Tan(Math.PI / 4 - phi / 2) / Math.Pow((1 - sp) / (1 + sp), E / 2);
        double rho   = A * LccF * Math.Pow(t, LccN);
        double theta = LccN * (lam - LccLon0);

        double x_m = rho * Math.Sin(theta) + LccFe_m;
        double y_m = LccRho0 - rho * Math.Cos(theta) + LccFn_m;

        return (x_m * MToFt, y_m * MToFt);
    }

    // ── Private helpers ─────────────────────────────────────────────────────

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
