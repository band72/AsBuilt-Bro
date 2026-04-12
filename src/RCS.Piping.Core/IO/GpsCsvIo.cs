using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RCS.Geo.Core;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.IO;

/// <summary>
/// Import and export GPS coordinate files (CSV / TXT) containing
/// Latitude, Longitude, Elevation, and Description for each point.
///
/// Supported import formats (auto-detected):
///   • With header:   Point,Latitude,Longitude,Elevation,Description
///   • Without header (4-5 cols):  numeric lat, numeric lon, elev, desc
///   • Point number in column 0 is optional — auto-numbered if absent or non-numeric.
///
/// Export produces two files:
///   • *_GPS.csv  — full record: Point,N,E,Elev,Lat,Lon,LatDMS,LonDMS,Desc
///   • *_LatLon.txt — compact: Pt,Lat,Lon,Desc  (for GIS tools, copy-paste)
/// </summary>
public static class GpsCsvIo
{
    // ─────────────────────────────────────────────────────────────────────────
    // Import
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Read a GPS CSV/TXT file and return a list of parsed rows.
    /// Throws <see cref="InvalidDataException"/> if the file cannot be parsed.
    /// </summary>
    public static List<GpsPointRecord> Import(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"GPS file not found: {filePath}");

        var lines  = File.ReadAllLines(filePath, Encoding.UTF8);
        var result = new List<GpsPointRecord>();
        int autoId = 1;

        bool headerSkipped = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            var cols = SplitCsv(line);
            if (cols.Length < 2) continue;

            // Detect and skip header row (first row, non-numeric first two data cols)
            if (!headerSkipped)
            {
                headerSkipped = true;
                if (!double.TryParse(cols[0].Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out _) &&
                    !double.TryParse(cols[1].Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out _))
                    continue;   // header row — skip
            }

            // Determine column layout
            // Layout A: Point, Lat, Lon [, Elev [, Desc]]
            // Layout B: Lat, Lon [, Elev [, Desc]]  (no point #)
            string pointId;
            double lat, lon, elev = 0;
            string desc = string.Empty;

            int offset = 0;
            if (TryParseD(cols[0], out double testVal) &&
                testVal > 90 || !TryParseD(cols[0], out _))
            {
                // col 0 is a point number (string or large number)
                pointId = cols[0].Trim();
                offset  = 1;
            }
            else
            {
                pointId = (autoId++).ToString();
            }

            if (cols.Length <= offset + 1) continue;

            // Try col[offset] = Lat, col[offset+1] = Lon
            if (!TryParseD(cols[offset],     out lat)) continue;
            if (!TryParseD(cols[offset + 1], out lon)) continue;

            if (cols.Length > offset + 2) TryParseD(cols[offset + 2], out elev);
            if (cols.Length > offset + 3) desc = cols[offset + 3].Trim().Trim('"');

            // Basic range validation
            if (lat is < -90 or > 90 || lon is < -180 or > 180) continue;

            result.Add(new GpsPointRecord(pointId, lat, lon, elev, desc));
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Export utilities
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Export all PointRows from the job to a full GPS CSV file.
    /// The primary lat/lon columns reflect <paramref name="useDms"/>:
    ///   false (default) → decimal degrees (7 dp);  true → DMS string.
    /// The DMS columns are always written regardless.
    /// <paramref name="zone"/> is passed to <see cref="StatePlaneProjection.ToLatLon(double,double,string)"/>.
    /// </summary>
    public static void ExportFullCsv(AsBuiltJob job, string outputPath, bool useDms = false, string zone = "EPSG:2236")
    {
        using var sw = new StreamWriter(outputPath, false, new UTF8Encoding(true));
        sw.WriteLine("Point,Northing,Easting,Elevation,Latitude,Longitude,LatitudeDMS,LongitudeDMS,Description");

        foreach (var row in job.PointRows)
        {
            var (lat, lon) = StatePlaneProjection.ToLatLon(row.Easting, row.Northing, zone);
            string latDms  = StatePlaneProjection.ToDms(lat, isLatitude: true);
            string lonDms  = StatePlaneProjection.ToDms(lon, isLatitude: false);
            string latOut  = useDms ? Esc(latDms) : $"{lat:F7}";
            string lonOut  = useDms ? Esc(lonDms) : $"{lon:F7}";
            sw.WriteLine(string.Join(",",
                Esc(row.PointId),
                $"{row.Northing:F3}",
                $"{row.Easting:F3}",
                $"{row.Elevation:F3}",
                latOut,
                lonOut,
                Esc(latDms),
                Esc(lonDms),
                Esc(row.Description)));
        }
    }

    /// <summary>
    /// Export a compact Lat/Lon TXT file (Point, Lat, Lon, Desc).
    /// When <paramref name="useDms"/> is true, outputs DMS strings instead of decimal degrees.
    /// <paramref name="zone"/> is passed to <see cref="StatePlaneProjection.ToLatLon(double,double,string)"/>.
    /// </summary>
    public static void ExportLatLonTxt(AsBuiltJob job, string outputPath, bool useDms = false, string zone = "EPSG:2236")
    {
        using var sw = new StreamWriter(outputPath, false, new UTF8Encoding(true));
        sw.WriteLine($"# GPS Coordinate Export — {job.Identity.JobNumber}");
        sw.WriteLine($"# Generated: {DateTime.Now:MM/dd/yyyy HH:mm}");
        sw.WriteLine($"# Projection: {zone} → WGS84");
        sw.WriteLine(useDms
            ? "# Point,LatitudeDMS,LongitudeDMS,Description"
            : "# Point,Latitude,Longitude,Description");

        foreach (var row in job.PointRows)
        {
            var (lat, lon) = StatePlaneProjection.ToLatLon(row.Easting, row.Northing, zone);
            if (useDms)
            {
                string latDms = StatePlaneProjection.ToDms(lat, isLatitude: true);
                string lonDms = StatePlaneProjection.ToDms(lon, isLatitude: false);
                sw.WriteLine($"{row.PointId},{latDms},{lonDms},{row.Description}");
            }
            else
            {
                sw.WriteLine($"{row.PointId},{lat:F7},{lon:F7},{row.Description}");
            }
        }
    }

    /// <summary>
    /// Convert a list of <see cref="GpsPointRecord"/> (from import) into PointRows
    /// by projecting Lat/Lon back to FL State Plane East (Northing/Easting).
    /// Existing rows with matching PointIds are overwritten; new ones are appended.
    /// </summary>
    public static int MergeIntoJob(IList<GpsPointRecord> records, AsBuiltJob job)
    {
        int added = 0;
        foreach (var rec in records)
        {
            var (eFt, nFt) = StatePlaneProjection.ToStatePlane(rec.Latitude, rec.Longitude);
            var existing   = job.PointRows.FirstOrDefault(p =>
                p.PointId.Equals(rec.PointId, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Northing    = nFt;
                existing.Easting     = eFt;
                existing.Elevation   = rec.Elevation;
                existing.Description = rec.Description;
            }
            else
            {
                job.PointRows.Add(new PointRow
                {
                    PointId     = rec.PointId,
                    Northing    = nFt,
                    Easting     = eFt,
                    Elevation   = rec.Elevation,
                    Description = rec.Description
                });
                added++;
            }
        }
        return added;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static bool TryParseD(string s, out double val)
        => double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any,
               System.Globalization.CultureInfo.InvariantCulture, out val);

    private static string Esc(string s)
        => s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;

    /// <summary>Split a CSV line, respecting double-quoted fields.</summary>
    private static string[] SplitCsv(string line)
    {
        var result  = new List<string>();
        bool inQ    = false;
        var  field  = new StringBuilder();
        foreach (char c in line)
        {
            if (c == '"') { inQ = !inQ; continue; }
            if (c == ',' && !inQ) { result.Add(field.ToString()); field.Clear(); }
            else field.Append(c);
        }
        result.Add(field.ToString());
        return result.ToArray();
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Data Transfer Object
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Parsed GPS point from an import file.</summary>
public sealed record GpsPointRecord(
    string PointId,
    double Latitude,
    double Longitude,
    double Elevation,
    string Description);
