using System;
using System.Collections.Generic;
using System.Text;

namespace RCS.Geo.Core;

/// <summary>
/// Generates a KML 2.2 document from a list of surveyed COGO points
/// for display in Google Earth, Google Maps, and any KML-aware GIS tool.
///
/// Each point becomes a <c>&lt;Placemark&gt;</c> with:
///   Name        = COGO point ID (e.g. "P1")
///   Description = user description + Northing/Easting/Elevation
///   coordinates = longitude,latitude,elevation_m (KML coordinate order)
///
/// Common usage:
/// <code>
///   var pts = Points.Select(p =>
///       (p.Id, p.Desc, StatePlaneProjection.ToLatLon(p.E, p.N, zone), p.Elev)).ToList();
///   KmlExporter.Export("output.kml", pts);
/// </code>
/// </summary>
public static class KmlExporter
{
    // KML spec: coordinates are lon,lat,alt (altitude in metres)
    private const double FtToM = 1_200.0 / 3_937.0;

    /// <summary>
    /// Writes a KML file from the supplied point list.
    /// </summary>
    /// <param name="outputPath">Full path of the .kml file to create / overwrite.</param>
    /// <param name="points">
    ///   Sequence of (id, description, (lat°, lon°), elevationFt) tuples.
    ///   Lat/Lon must be WGS84 decimal degrees; elevation in US survey feet.
    /// </param>
    /// <param name="documentName">Optional KML Document name shown in Google Earth sidebar.</param>
    public static void Export(
        string outputPath,
        IEnumerable<(string Id, string Description, (double Lat, double Lon) LatLon, double ElevFt)> points,
        string documentName = "COGO Survey Points")
    {
        var sb = new StringBuilder();

        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<kml xmlns=\"http://www.opengis.net/kml/2.2\">");
        sb.AppendLine("  <Document>");
        sb.AppendLine($"    <name>{XmlEsc(documentName)}</name>");
        sb.AppendLine($"    <description>Exported {DateTime.Now:MM/dd/yyyy HH:mm} by RCS Cogo Enterprise</description>");

        // Shared style: yellow pushpin with label
        sb.AppendLine("    <Style id=\"cogoPoint\">");
        sb.AppendLine("      <IconStyle>");
        sb.AppendLine("        <color>ff00d4ff</color>");    // amber/gold
        sb.AppendLine("        <scale>1.0</scale>");
        sb.AppendLine("        <Icon><href>http://maps.google.com/mapfiles/kml/pushpin/ylw-pushpin.png</href></Icon>");
        sb.AppendLine("      </IconStyle>");
        sb.AppendLine("      <LabelStyle><scale>0.8</scale></LabelStyle>");
        sb.AppendLine("    </Style>");

        int count = 0;
        foreach (var (id, desc, (lat, lon), elevFt) in points)
        {
            double elevM = elevFt * FtToM;
            string safeDesc =
                $"Point: {XmlEsc(id)}\n" +
                $"Description: {XmlEsc(desc)}\n" +
                $"Latitude: {lat:F7}°\n" +
                $"Longitude: {lon:F7}°\n" +
                $"Elevation: {elevFt:F3} ft ({elevM:F3} m)";

            sb.AppendLine("    <Placemark>");
            sb.AppendLine($"      <name>{XmlEsc(id)}</name>");
            sb.AppendLine($"      <description>{XmlEsc(safeDesc)}</description>");
            sb.AppendLine("      <styleUrl>#cogoPoint</styleUrl>");
            sb.AppendLine("      <Point>");
            sb.AppendLine($"        <coordinates>{lon:F7},{lat:F7},{elevM:F3}</coordinates>");
            sb.AppendLine("      </Point>");
            sb.AppendLine("    </Placemark>");
            count++;
        }

        sb.AppendLine("  </Document>");
        sb.AppendLine("</kml>");

        System.IO.File.WriteAllText(outputPath, sb.ToString(), new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    // XML-safe escape for name/description content
    private static string XmlEsc(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        return s
            .Replace("&",  "&amp;")
            .Replace("<",  "&lt;")
            .Replace(">",  "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'",  "&apos;");
    }
}
