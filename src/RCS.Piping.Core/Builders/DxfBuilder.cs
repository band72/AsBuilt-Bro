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
        // Water
        ("WATER",       "W-MAIN",        4),
        ("POTABLE",     "W-MAIN",        4),
        ("WM",          "W-MAIN",        4),
        // Wastewater gravity
        ("WASTEWATER",  "WW-MAIN",       3),
        ("SEWER",       "WW-MAIN",       3),
        ("GRAVITY",     "WW-MAIN",       3),
        ("WWG",         "WW-MAIN",       3),
        // Wastewater pressure / force main
        ("FORCE",       "WW-FORCE-MAIN", 82),
        ("PRESSURE",    "WW-FORCE-MAIN", 82),
        ("WWP",         "WW-FORCE-MAIN", 82),
        // Reclaimed
        ("RECLAIM",     "RCL-MAIN",      6),
        ("REUSE",       "RCL-MAIN",      6),
        ("RCL",         "RCL-MAIN",      6),
        // Storm
        ("STORM",       "ST-MAIN",       2),
        ("ST",          "ST-MAIN",       2),
        ("DRAIN",       "ST-MAIN",       2),
        // Electric
        ("ELECTRIC",    "E-CONDUIT",     20),
        ("CONDUIT",     "E-CONDUIT",     20),
        ("DUCT",        "E-CONDUIT",     20),
        ("ELECTRIC",    "E-CONDUIT",     20),
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
            WriteText(sb, pt.Easting, pt.Northing + 3.0, st.Type, stLayer, 0.6);
            if (st.RimElevation.HasValue)
                WriteText(sb, pt.Easting, pt.Northing - 3.0,
                          $"Rim={st.RimElevation:F2}'", LayerLabels, 0.5);
        }

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

    private static void WriteTitleBlock(StringBuilder sb, AsBuiltJob job)
    {
        double bx = -200.0, by = -50.0;
        double ht = 1.2;

        WriteText(sb, bx, by + ht * 5, $"JOB: {job.Identity.JobNumber}",      LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 4, $"CLIENT: {job.Identity.ClientName}",   LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 3, $"DATE: {job.Identity.FieldDate:MM/dd/yyyy}", LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 2, $"DRAFTER: {job.Identity.Drafter}",     LayerAnnot, ht);
        WriteText(sb, bx, by + ht * 1, $"CHECKER: {job.Identity.Checker}",     LayerAnnot, ht);
        WriteText(sb, bx, by,          $"REV: {job.Identity.RevisionNumber}",   LayerAnnot, ht);
    }

    private static void WriteEof(StringBuilder sb) => sb.AppendLine("  0\nEOF");
}
