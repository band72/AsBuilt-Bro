using System;
using System.Collections.Generic;
using System.Linq;
using RCS.Cogo.Core.Primitives;

namespace RCS.Alignments.Core;

/// <summary>One point on the alignment centerline at a computed station.</summary>
public class StationPoint
{
    public double Station { get; set; }
    public Point3D Coordinate { get; set; } = new(0, 0, 0);
    public double EGElevation { get; set; }   // existing ground from profile
    public double FGElevation { get; set; }   // finished grade from profile
    public double CutFill => FGElevation - EGElevation; // + = fill, - = cut

    /// <summary>Station formatted as highway notation e.g. "10+25.00"</summary>
    public string Label => FormatStation(Station);

    public static string FormatStation(double station)
    {
        int hundreds = (int)(station / 100);
        double remainder = station - hundreds * 100.0;
        return $"{hundreds}+{remainder:00.00}";
    }
}

/// <summary>A single ground shot captured perpendicular to the alignment centerline.</summary>
public record XsShot(double Offset, double EGElevation);

/// <summary>Full cross-section data at one station.</summary>
public class CrossSection
{
    public double Station { get; set; }
    public string StationLabel => StationPoint.FormatStation(Station);
    public Point3D CenterlinePoint { get; set; } = new(0, 0, 0);
    public double FGElevation { get; set; }      // design elevation at CL
    public double EGElevationCL { get; set; }    // ground elevation at CL
    public double CutFill => FGElevation - EGElevationCL;  // + fill / - cut

    /// <summary>Road template half-widths (left = negative offset side)</summary>
    public double TemplateWidthLeft  { get; set; } = 12.0;
    public double TemplateWidthRight { get; set; } = 12.0;

    /// <summary>Foreslope ratios H:V (e.g. 2.0 = 2:1). Positive value.</summary>
    public double ForeslopeLeft  { get; set; } = 2.0;
    public double ForeslopeRight { get; set; } = 2.0;

    /// <summary>Collected ground shots across the section, sorted by offset.</summary>
    public List<XsShot> Shots { get; } = new();

    /// <summary>Ground elevation at a given offset, interpolated from nearest shots.</summary>
    public double? GetGroundElevAt(double offset)
    {
        if (Shots.Count == 0) return null;

        var sorted = Shots.OrderBy(s => s.Offset).ToList();
        if (offset <= sorted[0].Offset) return sorted[0].EGElevation;
        if (offset >= sorted[^1].Offset) return sorted[^1].EGElevation;

        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var a = sorted[i];
            var b = sorted[i + 1];
            if (offset >= a.Offset && offset <= b.Offset)
            {
                double t = (offset - a.Offset) / (b.Offset - a.Offset);
                return a.EGElevation + t * (b.EGElevation - a.EGElevation);
            }
        }
        return null;
    }

    /// <summary>
    /// Computes the daylight point offset (where foreslope meets natural ground).
    /// Returns offset from CL and elevation at daylight.
    /// </summary>
    public (double Offset, double Elevation)? GetDaylightPoint(bool rightSide)
    {
        if (Shots.Count < 2) return null;

        double sign = rightSide ? 1.0 : -1.0;
        double slope = rightSide ? ForeslopeRight : ForeslopeLeft;
        double roadEdgeOffset = sign * (rightSide ? TemplateWidthRight : TemplateWidthLeft);
        double roadEdgeElev = FGElevation; // assume flat template for now

        // Walk from road edge outward in 0.5 ft increments to find daylight
        for (double off = roadEdgeOffset; Math.Abs(off) <= 200; off += sign * 0.5)
        {
            double groundElev = GetGroundElevAt(off) ?? double.NaN;
            if (double.IsNaN(groundElev)) break;

            // Foreslope elevation at this offset from road edge
            double slopeElev = roadEdgeElev - (Math.Abs(off) - Math.Abs(roadEdgeOffset)) / slope;

            if (CutFill <= 0) // Cut — foreslope rises above ground
            {
                if (groundElev >= slopeElev)
                    return (off, groundElev);
            }
            else // Fill — foreslope drops below ground
            {
                if (groundElev <= slopeElev)
                    return (off, groundElev);
            }
        }
        return null;
    }
}

/// <summary>Engine that generates station points and cross-sections from an alignment.</summary>
public static class StationingEngine
{
    /// <summary>
    /// Generates a list of station points at uniform intervals along the alignment.
    /// Always includes the start and end stations.
    /// </summary>
    public static List<StationPoint> StationAlignment(Alignment alignment, double interval, Profile? fgProfile = null, Profile? egProfile = null)
    {
        var results = new List<StationPoint>();
        if (alignment.Elements.Count == 0) return results;

        double startSta = alignment.Elements.First().StartStation;
        double endSta   = alignment.Elements.Last().EndStation;

        double sta = startSta;
        while (sta <= endSta + 1e-6)
        {
            var coord = alignment.GetCoordinateAt(sta);
            if (coord != null)
            {
                results.Add(new StationPoint
                {
                    Station     = sta,
                    Coordinate  = coord,
                    FGElevation = fgProfile?.GetElevationAtStation(sta) ?? coord.Elevation,
                    EGElevation = egProfile?.GetElevationAtStation(sta) ?? coord.Elevation,
                });
            }
            if (sta >= endSta) break;
            sta = Math.Min(sta + interval, endSta);
        }

        return results;
    }

    /// <summary>
    /// Given a set of cross section ground shots, builds a CrossSection object
    /// for each station in the provided list.
    /// </summary>
    public static List<CrossSection> BuildCrossSections(
        Alignment alignment,
        List<StationPoint> stationPoints,
        List<(double Station, double Offset, double Elevation)> groundShots,
        double templateWidthLeft  = 12.0,
        double templateWidthRight = 12.0,
        double foreslopeLeft      = 2.0,
        double foreslopeRight     = 2.0)
    {
        var results = new List<CrossSection>();

        foreach (var sp in stationPoints)
        {
            var xs = new CrossSection
            {
                Station            = sp.Station,
                CenterlinePoint    = sp.Coordinate,
                FGElevation        = sp.FGElevation,
                EGElevationCL      = sp.EGElevation,
                TemplateWidthLeft  = templateWidthLeft,
                TemplateWidthRight = templateWidthRight,
                ForeslopeLeft      = foreslopeLeft,
                ForeslopeRight     = foreslopeRight,
            };

            // Assign ground shots for this station (within 1 ft tolerance)
            foreach (var (sta, offset, elev) in groundShots)
            {
                if (Math.Abs(sta - sp.Station) < 1.0)
                    xs.Shots.Add(new XsShot(offset, elev));
            }

            // Ensure a CL shot exists
            if (!xs.Shots.Any(s => Math.Abs(s.Offset) < 0.01))
                xs.Shots.Add(new XsShot(0.0, sp.EGElevation));

            results.Add(xs);
        }

        return results;
    }

    /// <summary>Computes cut/fill area for a cross-section using the prismoidal formula approximation.</summary>
    public static double ComputeAreaAtSection(CrossSection xs, bool cutArea = true)
    {
        // Simple trapezoidal integration over shot offsets
        if (xs.Shots.Count < 2) return 0.0;

        var shots = xs.Shots.OrderBy(s => s.Offset).ToList();
        double area = 0.0;

        for (int i = 0; i < shots.Count - 1; i++)
        {
            double w = shots[i + 1].Offset - shots[i].Offset;
            double fgLeft  = xs.FGElevation;
            double fgRight = xs.FGElevation;
            double egLeft  = shots[i].EGElevation;
            double egRight = shots[i + 1].EGElevation;

            double cfLeft  = fgLeft  - egLeft;
            double cfRight = fgRight - egRight;

            if (cutArea)
            {
                double h1 = Math.Max(0, -cfLeft);
                double h2 = Math.Max(0, -cfRight);
                area += 0.5 * (h1 + h2) * Math.Abs(w);
            }
            else
            {
                double h1 = Math.Max(0, cfLeft);
                double h2 = Math.Max(0, cfRight);
                area += 0.5 * (h1 + h2) * Math.Abs(w);
            }
        }
        return area;
    }

    /// <summary>Generates a cut/fill summary table as formatted text for the COGO output log.</summary>
    public static string GenerateCutFillReport(List<CrossSection> sections)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("==========================================================================");
        sb.AppendLine("   CUT / FILL SUMMARY REPORT");
        sb.AppendLine("==========================================================================");
        sb.AppendLine($"  {"Station",-12} {"FG Elev",10} {"EG Elev",10} {"Cut/Fill",10} {"Status",-8}");
        sb.AppendLine("  " + new string('-', 56));

        double totalCutArea  = 0;
        double totalFillArea = 0;

        CrossSection? prev = null;
        for (int i = 0; i < sections.Count; i++)
        {
            var xs = sections[i];
            string status = xs.CutFill < -0.01 ? "CUT" : xs.CutFill > 0.01 ? "FILL" : "GRADE";
            sb.AppendLine($"  {xs.StationLabel,-12} {xs.FGElevation,10:F3} {xs.EGElevationCL,10:F3} {xs.CutFill,10:+0.000;-0.000} {status,-8}");

            if (prev != null)
            {
                double dist = xs.Station - prev.Station;
                double cutA  = (ComputeAreaAtSection(prev, true)  + ComputeAreaAtSection(xs, true))  / 2.0 * dist;
                double fillA = (ComputeAreaAtSection(prev, false) + ComputeAreaAtSection(xs, false)) / 2.0 * dist;
                totalCutArea  += cutA;
                totalFillArea += fillA;
            }
            prev = xs;
        }

        sb.AppendLine("  " + new string('-', 56));
        sb.AppendLine($"  {"Total Cut Volume:",-28} {totalCutArea / 27.0,10:F2} CY");
        sb.AppendLine($"  {"Total Fill Volume:",-28} {totalFillArea / 27.0,10:F2} CY");
        sb.AppendLine("==========================================================================");
        return sb.ToString();
    }
}
