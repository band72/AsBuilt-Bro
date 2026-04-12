using System;
using System.Globalization;
using System.IO;
using System.Text;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Builders;

// ─────────────────────────────────────────────────────────────────────────────
// PnezdExportBuilder
// Writes a standard PNEZD CSV file from the job's PointRows.
// Format: Point#, Northing, Easting, Elevation, Description
// Compatible with Civil 3D, Carlson, and any COGO engine's PT/NEZ import.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class PnezdExportBuilder
{
    private const string Header = "Point,Northing,Easting,Elevation,Description";

    public void Build(AsBuiltJob job, string outputPath)
    {
        var sb = new StringBuilder(4096);
        sb.AppendLine(Header);

        foreach (var row in job.PointRows)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(row.PointId),
                row.Northing .ToString("F6", CultureInfo.InvariantCulture),
                row.Easting  .ToString("F6", CultureInfo.InvariantCulture),
                row.Elevation.ToString("F4", CultureInfo.InvariantCulture),
                EscapeCsv(row.Description)
            ));
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
