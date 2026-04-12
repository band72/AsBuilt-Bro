using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.Wpf.Services;

/// <summary>
/// Parses the application's standard CSV point export format and returns imported points.
///
/// Expected CSV format (matches <c>ExportCogoPointsCommand</c> output):
/// <code>
/// Point Number,Northing,Easting,Elevation,Description
/// 1,1234567.8900,987654.3200,45.0000,IRON PIN
/// </code>
/// Rules:
///   • First row is a header — always skipped.
///   • Lines beginning with <c>#</c> or blank lines are ignored.
///   • Values are trimmed; Description may contain embedded commas if quoted.
///   • Duplicate point IDs: new value overwrites existing (same behaviour as <c>AddPoint</c>).
/// </summary>
public static class CsvPointImporter
{
    public sealed record ImportResult(
        List<(string Id, double Northing, double Easting, double Elevation, string Description)> Points,
        List<string> Errors
    );

    /// <summary>Parses <paramref name="csvPath"/> and returns parsed points + any parse errors.</summary>
    public static ImportResult Parse(string csvPath)
    {
        var points = new List<(string, double, double, double, string)>();
        var errors = new List<string>();
        int lineNum = 0;

        foreach (var rawLine in File.ReadLines(csvPath))
        {
            lineNum++;
            var line = rawLine.Trim();

            // Skip header, blanks, comments
            if (lineNum == 1) continue;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

            // Respect quoted fields (Description may contain commas)
            var fields = SplitCsv(line);
            if (fields.Count < 4)
            {
                errors.Add($"Line {lineNum}: expected ≥4 fields, got {fields.Count} — skipped.");
                continue;
            }

            string id    = fields[0].Trim();
            string descr = fields.Count >= 5 ? fields[4].Trim() : string.Empty;

            if (string.IsNullOrWhiteSpace(id))
            {
                errors.Add($"Line {lineNum}: empty point ID — skipped.");
                continue;
            }

            if (!double.TryParse(fields[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double northing) ||
                !double.TryParse(fields[2], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double easting))
            {
                errors.Add($"Line {lineNum}: invalid Northing/Easting — skipped.");
                continue;
            }

            double.TryParse(fields[3], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double elev);

            points.Add((id, northing, easting, elev, descr));
        }

        return new ImportResult(points, errors);
    }

    // ── Minimal RFC-4180-compliant CSV split ──────────────────────────────────
    private static List<string> SplitCsv(string line)
    {
        var fields = new List<string>();
        int i = 0;
        while (i < line.Length)
        {
            if (line[i] == '"')
            {
                i++;
                var sb = new System.Text.StringBuilder();
                while (i < line.Length)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i += 2; }
                        else { i++; break; }
                    }
                    else sb.Append(line[i++]);
                }
                fields.Add(sb.ToString());
                if (i < line.Length && line[i] == ',') i++;
            }
            else
            {
                int end = line.IndexOf(',', i);
                if (end < 0) { fields.Add(line[i..]); break; }
                fields.Add(line[i..end]);
                i = end + 1;
            }
        }
        if (line.EndsWith(',')) fields.Add(string.Empty);
        return fields;
    }
}
