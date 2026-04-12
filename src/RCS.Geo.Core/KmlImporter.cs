using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RCS.Geo.Core;

/// <summary>
/// Parses KML 2.2 documents and extracts <c>&lt;Placemark&gt;</c> points.
/// Each Placemark's WGS84 (lon, lat, alt) coordinates are inverted to US survey feet
/// in the specified FL State Plane zone via <see cref="StatePlaneProjection"/>.
///
/// Handles:
///   - Default KML namespace (http://www.opengis.net/kml/2.2)
///   - Namespace-less KML (some export tools omit the namespace)
///   - Alt-elevation missing → 0 ft
///   - Name missing → auto-generated "P{n}"
///   - Description text extracted and used as point description
/// </summary>
public static class KmlImporter
{
    // KML elevation is in metres; convert to US survey feet
    private const double MToFt = 3937.0 / 1200.0;

    // KML standard namespace; also try no-namespace fallback
    private static readonly XNamespace KmlNs   = "http://www.opengis.net/kml/2.2";
    private static readonly XNamespace KmlNs22 = "http://earth.google.com/kml/2.2";  // legacy Google Earth

    /// <summary>
    /// Represents a single imported KML point, pre-projected to State Plane feet.
    /// </summary>
    public sealed record KmlPoint(
        string Id,
        string Description,
        double Northing,    // US survey feet
        double Easting,     // US survey feet
        double ElevFt       // converted from KML metres
    );

    /// <summary>
    /// Imports all <c>&lt;Placemark&gt;</c> points from a KML file.
    /// </summary>
    /// <param name="kmlPath">Absolute path of the .kml file to read.</param>
    /// <param name="zone">
    ///   EPSG zone string (e.g. "EPSG:2236"). Pass the result of
    ///   <see cref="StatePlaneProjection.NormalizeZone"/> for the active project zone.
    /// </param>
    /// <param name="startingPointId">
    ///   Base ID for auto-numbered points when the Placemark has no name.
    ///   Defaults to "KML1", "KML2", …
    /// </param>
    /// <returns>List of projected points in insertion order.</returns>
    public static List<KmlPoint> Import(string kmlPath, string zone = "EPSG:2236", int startingPointId = 1)
    {
        var doc = XDocument.Load(kmlPath);
        var root = doc.Root ?? throw new InvalidOperationException("Empty KML file.");

        // Resolve namespace: try standard, legacy, then no-NS
        XNamespace ns;
        var rootNs = root.Name.Namespace;
        if (rootNs == KmlNs || rootNs == KmlNs22)
            ns = rootNs;
        else
            ns = XNamespace.None;

        var results  = new List<KmlPoint>();
        int autoIdx  = startingPointId;

        // Walk ALL Placemark elements regardless of nesting depth
        foreach (var pm in doc.Descendants(ns + "Placemark"))
        {
            // Only process Placemarks that contain a Point geometry
            var pointEl = pm.Element(ns + "Point");
            if (pointEl == null) continue;

            var coordEl = pointEl.Element(ns + "coordinates");
            if (coordEl == null) continue;

            // KML coordinate string: "lon,lat[,alt]  lon2,lat2[,alt2] ..."
            // For a Point there is exactly one coordinate triple
            var raw = coordEl.Value.Trim();
            if (!TryParseCoordinate(raw, out double lon, out double lat, out double altM))
                continue;

            // Convert altitude (metres) → feet; default 0 when absent
            double elevFt = altM * MToFt;

            // Project WGS84 → State Plane
            var (easting, northing) = StatePlaneProjection.ToStatePlane(lat, lon, zone);

            // Name = Placemark/name text, stripped of whitespace
            string id = pm.Element(ns + "name")?.Value?.Trim() ?? $"KML{autoIdx++}";
            if (string.IsNullOrWhiteSpace(id)) id = $"KML{autoIdx++}";

            // Description — strip HTML tags if present (Google Earth injects HTML)
            string desc = StripHtml(pm.Element(ns + "description")?.Value ?? string.Empty);

            results.Add(new KmlPoint(id, desc, northing, easting, elevFt));
        }

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool TryParseCoordinate(string raw, out double lon, out double lat, out double alt)
    {
        lon = lat = alt = 0;

        // Take first coordinate triple (split on whitespace/comma)
        var parts = raw.Split(new[] { ',', ' ', '\t', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2) return false;

        if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out lon)) return false;
        if (!double.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out lat)) return false;
        if (parts.Length >= 3)
            double.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out alt);

        // Basic sanity: lat ∈ [-90,90], lon ∈ [-180,180]
        return Math.Abs(lat) <= 90 && Math.Abs(lon) <= 180;
    }

    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static string StripHtml(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        return HtmlTagRegex.Replace(s, " ").Trim();
    }
}
