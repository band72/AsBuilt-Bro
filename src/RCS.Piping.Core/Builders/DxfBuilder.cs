using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Builders;

// ─────────────────────────────────────────────────────────────────────────────
// DxfBuilder
// Writes a standards-compliant ASCII DXF R12/2000 file from an AsBuiltJob.
// Layers:
//   AS-BUILT-PIPES       → pipe run polylines, colour 5 (blue)
//   AS-BUILT-STRUCTURES  → circle + attribute block at each structure node
//   AS-BUILT-LABELS      → MTEXT labels (bearing, diameter, invert, length)
//   AS-BUILT-ANNOT       → title block text entities
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DxfBuilder
{
    private const string LayerPipes      = "AS-BUILT-PIPES";
    private const string LayerStructures = "AS-BUILT-STRUCTURES";
    private const string LayerLabels     = "AS-BUILT-LABELS";
    private const string LayerAnnot      = "AS-BUILT-ANNOT";

    // ── Entry Point ───────────────────────────────────────────────────────────

    public void Build(AsBuiltJob job, string outputPath)
    {
        var sb = new StringBuilder(8192);

        WriteHeader(sb, job);
        WriteLayers(sb);
        WriteEntitiesSection(sb, job);
        WriteEof(sb);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.ASCII);
    }

    // ── Sections ──────────────────────────────────────────────────────────────

    private static void WriteHeader(StringBuilder sb, AsBuiltJob job)
    {
        // Minimal but valid DXF header
        sb.AppendLine("  0\nSECTION");
        sb.AppendLine("  2\nHEADER");
        sb.AppendLine("  9\n$ACADVER");
        sb.AppendLine("  1\nAC1015");  // AutoCAD 2000
        sb.AppendLine("  9\n$INSUNITS");
        sb.AppendLine(" 70\n     2");  // feet
        sb.AppendLine("  9\n$MEASUREMENT");
        sb.AppendLine(" 70\n     0");  // English
        sb.AppendLine("  0\nENDSEC");
    }

    private static void WriteLayers(StringBuilder sb)
    {
        sb.AppendLine("  0\nSECTION");
        sb.AppendLine("  2\nTABLES");
        sb.AppendLine("  0\nTABLE");
        sb.AppendLine("  2\nLAYER");

        WriteLayer(sb, LayerPipes,      5);   // blue
        WriteLayer(sb, LayerStructures, 3);   // green
        WriteLayer(sb, LayerLabels,     7);   // white/black
        WriteLayer(sb, LayerAnnot,      1);   // red

        sb.AppendLine("  0\nENDTAB");
        sb.AppendLine("  0\nENDSEC");
    }

    private static void WriteLayer(StringBuilder sb, string name, int color)
    {
        sb.AppendLine("  0\nLAYER");
        sb.AppendLine($"  2\n{name}");
        sb.AppendLine($" 70\n     0");    // flags: 0 = on
        sb.AppendLine($" 62\n     {color}");
        sb.AppendLine($"  6\nCONTINUOUS");
    }

    private void WriteEntitiesSection(StringBuilder sb, AsBuiltJob job)
    {
        sb.AppendLine("  0\nSECTION");
        sb.AppendLine("  2\nENTITIES");

        // Build coordinate lookup: point# → (N, E, Z)
        var coords = job.PointRows.ToDictionary(
            r => r.PointId,
            r => (r.Northing, r.Easting, r.Elevation));

        // Draw pipe runs as LWPOLYLINE (2 vertices each)
        foreach (var run in job.Network.Runs.Values)
        {
            if (!coords.TryGetValue(run.FromPointId, out var from)) continue;
            if (!coords.TryGetValue(run.ToPointId,   out var to))   continue;

            WritePipeLine(sb, from.Easting, from.Northing, from.Elevation,
                              to.Easting,   to.Northing,   to.Elevation);

            // Label at midpoint
            double mx = (from.Easting  + to.Easting)  / 2.0;
            double my = (from.Northing + to.Northing) / 2.0;
            double dz = from.Elevation - to.Elevation;
            double dx = to.Easting  - from.Easting;
            double dy = to.Northing - from.Northing;
            double len = Math.Sqrt(dx * dx + dy * dy);
            string label = $"{run.Diameter}\" {run.Material}  L={len:F2}'  ΔZ={dz:+0.00;-0.00;0.00}'";
            WriteText(sb, mx, my, label, LayerLabels, 0.5);
        }

        // Draw structures as circles + text
        foreach (var st in job.Network.Structures.Values)
        {
            if (!coords.TryGetValue(st.PointId, out var pt)) continue;
            WriteCircle(sb, pt.Easting, pt.Northing, 2.5);
            WriteText(sb, pt.Easting, pt.Northing + 3.0, st.Type, LayerStructures, 0.6);
            if (st.RimElevation.HasValue)
                WriteText(sb, pt.Easting, pt.Northing - 3.0,
                          $"Rim={st.RimElevation:F2}'", LayerLabels, 0.5);
        }

        // Title block annotation
        WriteTitleBlock(sb, job);

        sb.AppendLine("  0\nENDSEC");
    }

    // ── Entity Helpers ────────────────────────────────────────────────────────

    private static void WritePipeLine(StringBuilder sb,
        double x1, double y1, double z1,
        double x2, double y2, double z2)
    {
        sb.AppendLine("  0\nLINE");
        sb.AppendLine($"  8\n{LayerPipes}");
        sb.AppendLine($" 62\n     5");
        sb.AppendLine($" 10\n{x1:F6}");
        sb.AppendLine($" 20\n{y1:F6}");
        sb.AppendLine($" 30\n{z1:F6}");
        sb.AppendLine($" 11\n{x2:F6}");
        sb.AppendLine($" 21\n{y2:F6}");
        sb.AppendLine($" 31\n{z2:F6}");
    }

    private static void WriteCircle(StringBuilder sb, double cx, double cy, double r)
    {
        sb.AppendLine("  0\nCIRCLE");
        sb.AppendLine($"  8\n{LayerStructures}");
        sb.AppendLine($" 62\n     3");
        sb.AppendLine($" 10\n{cx:F6}");
        sb.AppendLine($" 20\n{cy:F6}");
        sb.AppendLine($" 30\n0.000000");
        sb.AppendLine($" 40\n{r:F6}");
    }

    private static void WriteText(StringBuilder sb, double x, double y, string text,
                                  string layer, double height)
    {
        sb.AppendLine("  0\nTEXT");
        sb.AppendLine($"  8\n{layer}");
        sb.AppendLine($" 10\n{x:F6}");
        sb.AppendLine($" 20\n{y:F6}");
        sb.AppendLine($" 30\n0.000000");
        sb.AppendLine($" 40\n{height:F4}");
        sb.AppendLine($"  1\n{text}");
    }

    private static void WriteTitleBlock(StringBuilder sb, AsBuiltJob job)
    {
        double bx = -200.0, by = -50.0;   // fixed lower-left anchor (adjustable)
        double ht = 1.2;

        WriteText(sb, bx, by + ht * 5, $"JOB: {job.Identity.JobNumber}", LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 4, $"CLIENT: {job.Identity.ClientName}", LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 3, $"DATE: {job.Identity.FieldDate:MM/dd/yyyy}", LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 2, $"DRAFTER: {job.Identity.Drafter}", LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 1, $"CHECKER: {job.Identity.Checker}", LayerAnnot, ht);
        WriteText(sb, bx, by,          $"REV: {job.Identity.RevisionNumber}", LayerAnnot, ht);
    }

    private static void WriteEof(StringBuilder sb)
    {
        sb.AppendLine("  0\nEOF");
    }
}
