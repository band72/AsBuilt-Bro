using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Builders;

// ─────────────────────────────────────────────────────────────────────────────
// DxfBuilder  v2 — Utility-layer-per-type
// Each utility discipline gets its own DXF layer with a distinct colour so the
// delivered DXF is immediately usable in AutoCAD / Civil 3D without re-layering.
//
// Layer mapping (AutoCAD colour index):
//   W-MAIN        → cyan    (4)    potable water mains
//   W-VALVE       → cyan    (4)    water valves / gate valves
//   W-HYDRANT     → cyan    (4)    fire hydrants
//   W-METER       → cyan    (4)    water meters
//   WW-MAIN       → green   (3)    wastewater gravity mains
//   WW-MH         → green   (3)    wastewater manholes
//   WW-FORCE-MAIN → 82            wastewater pressure / force main
//   RCL-MAIN      → purple  (6)    reclaimed / reuse water
//   ST-MAIN       → yellow  (2)    storm drainage pipes
//   ST-MH         → yellow  (2)    storm inlet / junction boxes
//   E-CONDUIT     → 20            electric conduit / duct bank
//   E-VAULT       → 20            electric vaults / pull boxes
//   G-MAIN        → 30            natural gas
//   TEL-MAIN      → 150           telecommunications conduit
//   AS-BUILT-STRUCTURES → 3       catch-all structures
//   AS-BUILT-LABELS     → 7       text labels
//   AS-BUILT-ANNOT      → 1       title block annotation
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DxfBuilder
{
    // ── Fixed utility-agnostic layers
    private const string LayerStructures = "AS-BUILT-STRUCTURES";
    private const string LayerLabels     = "AS-BUILT-LABELS";
    private const string LayerAnnot      = "AS-BUILT-ANNOT";

    // ── Maps a PipeRun.Type keyword → (DXF layer name, ACI colour index)
    // Matching is case-insensitive prefix/contains search.
    private static readonly (string keyword, string layer, int color)[] _pipeLayerMap =
    [
        // Wastewater pressure / force main  (must precede "SEWER" / "WASTE")
        ("FORCE",       "WW-FORCE-MAIN", 82),
        ("PRESSURE",    "WW-FORCE-MAIN", 82),
        ("WWP",         "WW-FORCE-MAIN", 82),
        // Wastewater gravity  (must precede generic WATER)
        ("WASTEWATER",  "WW-MAIN",       3),
        ("SEWER",       "WW-MAIN",       3),
        ("GRAVITY",     "WW-MAIN",       3),
        ("WWG",         "WW-MAIN",       3),
        // Potable water
        ("POTABLE",     "W-MAIN",        4),
        ("WATER",       "W-MAIN",        4),
        ("WM",          "W-MAIN",        4),
        // Reclaimed
        ("RECLAIM",     "RCL-MAIN",      6),
        ("REUSE",       "RCL-MAIN",      6),
        ("RCL",         "RCL-MAIN",      6),
        // Storm
        ("STORM",       "ST-MAIN",       2),
        ("DRAIN",       "ST-MAIN",       2),
        // Electric
        ("ELECTRIC",    "E-CONDUIT",     20),
        ("CONDUIT",     "E-CONDUIT",     20),
        ("DUCT",        "E-CONDUIT",     20),
        // Gas
        ("GAS",         "G-MAIN",        30),
        // Telecom
        ("TELECOM",     "TEL-MAIN",      150),
        ("FIBER",       "TEL-MAIN",      150),
        ("TEL",         "TEL-MAIN",      150),
    ];

    private static readonly (string keyword, string layer, int color)[] _structLayerMap =
    [
        ("VALVE",       "W-VALVE",       4),
        ("GATE",        "W-VALVE",       4),
        ("HYDRANT",     "W-HYDRANT",     4),
        ("METER",       "W-METER",       4),
        ("MANHOLE",     "WW-MH",         3),
        ("MH",          "WW-MH",         3),
        ("JUNCTION",    "ST-MH",         2),
        ("INLET",       "ST-MH",         2),
        ("VAULT",       "E-VAULT",       20),
        ("PULL",        "E-VAULT",       20),
    ];

    // ── Entry Point ───────────────────────────────────────────────────────────

    public void Build(AsBuiltJob job, string outputPath)
    {
        var sb = new StringBuilder(16384);

        // Collect all unique layers needed
        var usedLayers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var run in job.Network.Runs.Values)
        {
            var (layer, color) = ResolveLayerForRun(run.Type ?? "");
            usedLayers.TryAdd(layer, color);
        }
        foreach (var st in job.Network.Structures.Values)
        {
            var (layer, color) = ResolveLayerForStructure(st.Type ?? "");
            usedLayers.TryAdd(layer, color);
        }
        // Always include label/annot layers
        usedLayers.TryAdd(LayerLabels,  7);
        usedLayers.TryAdd(LayerAnnot,   1);

        WriteHeader(sb, job);
        WriteLayers(sb, usedLayers);
        WriteEntitiesSection(sb, job);
        WriteEof(sb);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.ASCII);
    }

    // ── Layer Resolution ──────────────────────────────────────────────────────

    private static (string layer, int color) ResolveLayerForRun(string type)
    {
        foreach (var (kw, layer, color) in _pipeLayerMap)
            if (type.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return (layer, color);
        return ("AS-BUILT-PIPES", 5);   // fallback: blue
    }

    private static (string layer, int color) ResolveLayerForStructure(string type)
    {
        foreach (var (kw, layer, color) in _structLayerMap)
            if (type.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return (layer, color);
        return (LayerStructures, 3);    // fallback: green
    }

    // ── Sections ──────────────────────────────────────────────────────────────

    private static void WriteHeader(StringBuilder sb, AsBuiltJob job)
    {
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

    private static void WriteLayers(StringBuilder sb, Dictionary<string, int> layers)
    {
        sb.AppendLine("  0\nSECTION");
        sb.AppendLine("  2\nTABLES");
        sb.AppendLine("  0\nTABLE");
        sb.AppendLine("  2\nLAYER");

        foreach (var (name, color) in layers)
            WriteLayer(sb, name, color);

        // Fallback pipe layer always present
        if (!layers.ContainsKey("AS-BUILT-PIPES"))
            WriteLayer(sb, "AS-BUILT-PIPES", 5);

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

        var coords = job.PointRows.ToDictionary(
            r => r.PointId,
            r => (r.Northing, r.Easting, r.Elevation));

        // Draw pipe runs — each on its discipline layer
        foreach (var run in job.Network.Runs.Values)
        {
            if (!coords.TryGetValue(run.FromPointId, out var from)) continue;
            if (!coords.TryGetValue(run.ToPointId,   out var to))   continue;

            var (layer, color) = ResolveLayerForRun(run.Type ?? "");
            WritePipeLine(sb, from.Easting, from.Northing, from.Elevation,
                              to.Easting,   to.Northing,   to.Elevation,
                              layer, color);

            // Label at midpoint
            double mx   = (from.Easting  + to.Easting)  / 2.0;
            double my   = (from.Northing + to.Northing) / 2.0;
            double dz   = from.Elevation - to.Elevation;
            double dx   = to.Easting  - from.Easting;
            double dy   = to.Northing - from.Northing;
            double len  = Math.Sqrt(dx * dx + dy * dy);
            string lbl  = $"{run.Diameter}\" {run.Material}  L={len:F2}'  \u0394Z={dz:+0.00;-0.00;0.00}'";
            WriteText(sb, mx, my, lbl, LayerLabels, 0.5);
        }

        // Draw structures — each on its discipline layer
        foreach (var st in job.Network.Structures.Values)
        {
            if (!coords.TryGetValue(st.PointId, out var pt)) continue;

            var (stLayer, stColor) = ResolveLayerForStructure(st.Type ?? "");
            WriteCircle(sb, pt.Easting, pt.Northing, 2.5, stLayer, stColor);
            WriteText(sb, pt.Easting, pt.Northing + 3.0, st.Type ?? string.Empty, stLayer, 0.6);
            if (st.RimElevation.HasValue)
                WriteText(sb, pt.Easting, pt.Northing - 3.0,
                          $"Rim={st.RimElevation:F2}'", LayerLabels, 0.5);
        }

        WriteProfileViews(sb, job);
        WriteTitleBlock(sb, job);
        sb.AppendLine("  0\nENDSEC");
    }

    // ── Entity Helpers ────────────────────────────────────────────────────────

    private static void WritePipeLine(StringBuilder sb,
        double x1, double y1, double z1,
        double x2, double y2, double z2,
        string layer, int color)
    {
        sb.AppendLine("  0\nLINE");
        sb.AppendLine($"  8\n{layer}");
        sb.AppendLine($" 62\n     {color}");
        sb.AppendLine($" 10\n{x1:F6}");
        sb.AppendLine($" 20\n{y1:F6}");
        sb.AppendLine($" 30\n{z1:F6}");
        sb.AppendLine($" 11\n{x2:F6}");
        sb.AppendLine($" 21\n{y2:F6}");
        sb.AppendLine($" 31\n{z2:F6}");
    }

    private static void WriteCircle(StringBuilder sb,
        double cx, double cy, double r, string layer, int color)
    {
        sb.AppendLine("  0\nCIRCLE");
        sb.AppendLine($"  8\n{layer}");
        sb.AppendLine($" 62\n     {color}");
        sb.AppendLine($" 10\n{cx:F6}");
        sb.AppendLine($" 20\n{cy:F6}");
        sb.AppendLine($" 30\n0.000000");
        sb.AppendLine($" 40\n{r:F6}");
    }

    private static void WriteText(StringBuilder sb, double x, double y,
        string text, string layer, double height)
    {
        sb.AppendLine("  0\nTEXT");
        sb.AppendLine($"  8\n{layer}");
        sb.AppendLine($" 10\n{x:F6}");
        sb.AppendLine($" 20\n{y:F6}");
        sb.AppendLine($" 30\n0.000000");
        sb.AppendLine($" 40\n{height:F4}");
        sb.AppendLine($"  1\n{text}");
    }

    private static void WriteTextWithPS(StringBuilder sb, double x, double y, string text, string layer, double height)
    {
        sb.AppendLine("  0\nTEXT");
        sb.AppendLine($"  8\n{layer}");
        sb.AppendLine(" 67\n     1"); // Paper Space
        sb.AppendLine($" 10\n{x:F6}");
        sb.AppendLine($" 20\n{y:F6}");
        sb.AppendLine($" 30\n0.000000");
        sb.AppendLine($" 40\n{height:F4}");
        sb.AppendLine($"  1\n{text}");
    }

    private static void WriteTitleBlock(StringBuilder sb, AsBuiltJob job)
    {
        // Original Model Space text fallback
        double bx = -200.0, by = -50.0;
        double ht = 1.2;
        WriteText(sb, bx, by + ht * 5, $"JOB: {job.Identity.JobNumber}",      LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 4, $"CLIENT: {job.Identity.ClientName}",   LayerAnnot, ht);
        
        // Advanced Paper Space P&P Generation Layout
        sb.AppendLine("  0\nVIEWPORT");
        sb.AppendLine("  8\nVIEWPORTS");
        sb.AppendLine(" 67\n     1"); // Paper Space
        sb.AppendLine(" 10\n17.0");   // Center X
        sb.AppendLine(" 20\n11.0");   // Center Y
        sb.AppendLine(" 30\n0.0");
        sb.AppendLine(" 40\n32.0");   // Width
        sb.AppendLine(" 41\n20.0");   // Height
        sb.AppendLine(" 68\n     2");   // ID
        sb.AppendLine(" 69\n     1");   // Status (on)
        sb.AppendLine(" 12\n0.0");    // View Center X
        sb.AppendLine(" 22\n0.0");    // View Center Y

        WriteTextWithPS(sb, 31.0,  3.0, $"BOUNDARY QC",                 LayerAnnot, 0.4);
        WriteTextWithPS(sb, 31.0,  2.5, $"JOB: {job.Identity.JobNumber}",      LayerAnnot, 0.3);
        WriteTextWithPS(sb, 31.0,  2.0, $"CLIENT: {job.Identity.ClientName}",   LayerAnnot, 0.3);
        WriteTextWithPS(sb, 31.0,  1.5, $"DATE: {job.Identity.FieldDate:MM/dd/yyyy}", LayerAnnot, 0.3);
        WriteTextWithPS(sb, 31.0,  1.0, $"DRAFTER: {job.Identity.Drafter}",     LayerAnnot, 0.3);
        WriteTextWithPS(sb, 31.0,  0.5, $"CHECKER: {job.Identity.Checker}",     LayerAnnot, 0.3);
        WriteTextWithPS(sb, 31.0,  0.0, $"REV: {job.Identity.RevisionNumber}",   LayerAnnot, 0.3);

        // Draw Paper Space Title Block borders
        sb.AppendLine("  0\nLWPOLYLINE");
        sb.AppendLine("  8\nTITLE_BLOCK");
        sb.AppendLine(" 67\n     1");
        sb.AppendLine(" 90\n     4");
        sb.AppendLine(" 70\n     1"); // Closed
        sb.AppendLine(" 10\n0.0"); sb.AppendLine(" 20\n0.0");
        sb.AppendLine(" 10\n34.0"); sb.AppendLine(" 20\n0.0");
        sb.AppendLine(" 10\n34.0"); sb.AppendLine(" 20\n22.0");
        sb.AppendLine(" 10\n0.0"); sb.AppendLine(" 20\n22.0");
    }

    private void WriteProfileViews(StringBuilder sb, AsBuiltJob job)
    {
        double xOffset = job.PointRows.Any() ? job.PointRows.Max(r => r.Easting) + 200 : 500;
        double yOffset = job.PointRows.Any() ? job.PointRows.Min(r => r.Northing) : 0;
        
        WriteText(sb, xOffset, yOffset + 50, "AS-BUILT STRUCTURAL PROFILES (5X VERTICAL EXAGGERATION)", LayerAnnot, 5.0);
        
        foreach (var run in job.Network.Runs.Values)
        {
            var p1 = job.PointRows.FirstOrDefault(r => r.PointId == run.FromPointId);
            var p2 = job.PointRows.FirstOrDefault(r => r.PointId == run.ToPointId);
            if (p1 == null || p2 == null) continue;
            
            double len = run.ComputedLength;
            if (len == 0) len = System.Math.Sqrt(System.Math.Pow(p2.Easting - p1.Easting, 2) + System.Math.Pow(p2.Northing - p1.Northing, 2));

            double startZ = run.InvertStart ?? p1.Elevation;
            double endZ   = run.InvertEnd ?? p2.Elevation;
            
            // Determine vertical baseline
            double baseZ = Math.Min(startZ, endZ) - 5;
            
            // Draw profile local axes
            WritePipeLine(sb, xOffset, yOffset, 0, xOffset + Math.Max(len, 50), yOffset, 0, LayerAnnot, 1); // X Axis
            WritePipeLine(sb, xOffset, yOffset, 0, xOffset, yOffset + 50, 0, LayerAnnot, 1); // Y Axis
            
            var (layer, color) = ResolveLayerForRun(run.Type ?? "");
            
            // X goes from 0 to len. Y is (elevation - baseZ) * Exaggeration (x5).
            WritePipeLine(sb, xOffset, yOffset + ((startZ - baseZ) * 5), 0, xOffset + len, yOffset + ((endZ - baseZ) * 5), 0, layer, color);
            
            WriteText(sb, xOffset, yOffset - 5, $"Profile: Run {run.Id} ({run.Type}) L={len:F2}ft", LayerAnnot, 2.0);
            WriteText(sb, xOffset, yOffset + ((startZ - baseZ) * 5) + 2, $"Inv: {startZ:F2}", LayerLabels, 1.0);
            WriteText(sb, xOffset + len, yOffset + ((endZ - baseZ) * 5) + 2, $"Inv: {endZ:F2}", LayerLabels, 1.0);
            
            // Render Crossing Interferences
            foreach (var r2 in job.Network.Runs.Values)
            {
                if (r2.Id == run.Id) continue;
                if (r2.FromPointId == run.FromPointId || r2.ToPointId == run.ToPointId || r2.FromPointId == run.ToPointId || r2.ToPointId == run.FromPointId) continue;

                var p3 = job.PointRows.FirstOrDefault(r => r.PointId == r2.FromPointId);
                var p4 = job.PointRows.FirstOrDefault(r => r.PointId == r2.ToPointId);
                if (p3 == null || p4 == null) continue;

                double E1 = p1.Easting, N1 = p1.Northing;
                double E2 = p2.Easting,   N2 = p2.Northing;
                double E3 = p3.Easting, N3 = p3.Northing;
                double E4 = p4.Easting,   N4 = p4.Northing;

                double denom = (N4 - N3) * (E2 - E1) - (E4 - E3) * (N2 - N1);
                if (Math.Abs(denom) < 1e-9) continue;

                double uA = ((E4 - E3) * (N1 - N3) - (N4 - N3) * (E1 - E3)) / denom;
                double uB = ((E2 - E1) * (N1 - N3) - (N2 - N1) * (E1 - E3)) / denom;

                if (uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1)
                {
                    double r2StartZ = r2.InvertStart ?? p3.Elevation;
                    double r2EndZ   = r2.InvertEnd ?? p4.Elevation;
                    double z2AtCross = r2StartZ + uB * (r2EndZ - r2StartZ);
                    
                    double zPlot = yOffset + ((z2AtCross - baseZ) * 5);
                    double xPlot = xOffset + (uA * len);
                    
                    var (r2Layer, r2Color) = ResolveLayerForRun(r2.Type ?? "");
                    
                    // Draw crossing as a scaled circle
                    double r2DiamFt = (r2.Diameter / 2.0) / 12.0;
                    WriteCircle(sb, xPlot, zPlot, Math.Max(0.5, r2DiamFt * 5), r2Layer, r2Color);
                    
                    // Draw vertical clearance line
                    double z1AtCross = startZ + uA * (endZ - startZ);
                    double zPlot1 = yOffset + ((z1AtCross - baseZ) * 5);
                    WritePipeLine(sb, xPlot, zPlot, 0, xPlot, zPlot1, 0, LayerAnnot, 8); // Gray line connecting them
                        
                    WriteText(sb, xPlot + 1.5, zPlot, $"X-ing Run {r2.Id} ({r2.Type} Inv:{z2AtCross:F2}')", LayerLabels, 0.8);
                }
            }

            yOffset += 100; // Next profile shifted up
        }
    }

    private static void WriteEof(StringBuilder sb) => sb.AppendLine("  0\nEOF");
}
