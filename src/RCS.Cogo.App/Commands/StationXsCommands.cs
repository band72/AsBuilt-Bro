using System;
using System.Linq;
using System.Threading.Tasks;
using RCS.Alignments.Core;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

/// <summary>
/// STATION command — query or list coordinates along an alignment by station.
///
/// Usage:
///   STATION &lt;AlignmentName&gt; &lt;Station&gt;
///       Report N/E/Elev at a given station (e.g. "2+50" or "250")
///
///   STATION &lt;AlignmentName&gt; PT &lt;PointId&gt;
///       Report the station + offset of a stored COGO point
///
///   STATION &lt;AlignmentName&gt; LIST &lt;FromSta&gt; &lt;ToSta&gt; &lt;Interval&gt;
///       Generate a station/coordinate table
/// </summary>
public class StationCommand : ICommand
{
    public string Name => "STATION";
    public string Description => "Query station/offset on an alignment. Usage: STATION <Algn> <Sta> | STATION <Algn> PT <PtId> | STATION <Algn> LIST <from> <to> <interval>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 3)
        {
            context.Log("Error: STATION requires at least alignment name and a station or subcommand.");
            context.Log("  STATION <Algn> <Station>");
            context.Log("  STATION <Algn> PT <PointId>");
            context.Log("  STATION <Algn> LIST <From> <To> <Interval>");
            return Task.CompletedTask;
        }

        string algnName = args[1];
        var alignment = context.GetAlignment(algnName);
        if (alignment == null)
        {
            context.Log($"Error: Alignment '{algnName}' not found.");
            return Task.CompletedTask;
        }

        string sub = args[2].ToUpperInvariant();

        // --- STATION <Algn> PT <PtId> ---
        if (sub == "PT")
        {
            if (args.Length < 4) { context.Log("Error: STATION PT requires a point ID."); return Task.CompletedTask; }
            var pt = context.GetPoint(args[3]);
            if (pt == null) { context.Log($"Error: Point {args[3]} not found."); return Task.CompletedTask; }

            var so = alignment.GetStationOffset(pt);
            if (so == null)
            {
                context.Log($"Point {args[3]} does not project onto alignment '{algnName}'.");
            }
            else
            {
                string staLabel = StationPoint.FormatStation(so.Value.Station);
                string offStr   = so.Value.Offset >= 0 ? $"{so.Value.Offset:F3} RT" : $"{Math.Abs(so.Value.Offset):F3} LT";
                context.Log($"Point {args[3]} → {algnName}  Sta: {staLabel}  Offset: {offStr}");
            }
            return Task.CompletedTask;
        }

        // --- STATION <Algn> LIST <From> <To> <Interval> ---
        if (sub == "LIST")
        {
            if (args.Length < 6)
            {
                context.Log("Error: STATION LIST requires: STATION <Algn> LIST <FromSta> <ToSta> <Interval>");
                return Task.CompletedTask;
            }

            double from     = ParseStation(args[3]);
            double to       = ParseStation(args[4]);
            double interval = ParseStation(args[5]);

            context.Log($"  Station Table — {algnName}");
            context.Log($"  {"Station",-12} {"Northing",12} {"Easting",12}");
            context.Log($"  {new string('-', 40)}");

            for (double sta = from; sta <= to + 1e-6; sta = Math.Min(sta + interval, to))
            {
                var coord = alignment.GetCoordinateAt(sta);
                if (coord != null)
                {
                    context.Log($"  {StationPoint.FormatStation(sta),-12} {coord.Northing,12:F3} {coord.Easting,12:F3}");
                }
                if (sta >= to) break;
            }
            return Task.CompletedTask;
        }

        // --- STATION <Algn> <StationValue> ---
        double station = ParseStation(sub);
        var coordinate = alignment.GetCoordinateAt(station);
        if (coordinate == null)
        {
            context.Log($"Error: Station {StationPoint.FormatStation(station)} is outside alignment '{algnName}' extents.");
            return Task.CompletedTask;
        }

        context.Log($"Alignment '{algnName}'  Sta: {StationPoint.FormatStation(station)}");
        context.Log($"  N: {coordinate.Northing:F4}   E: {coordinate.Easting:F4}");
        return Task.CompletedTask;
    }

    private static double ParseStation(string s)
    {
        // Accept "10+25.00" or "1025" or "1025.00"
        return double.TryParse(s.Replace("+", ""), out double v) ? v : 0;
    }
}

/// <summary>
/// XS command — define cross-section templates and ground shots for an alignment.
///
/// Usage:
///   XS BEG &lt;AlignmentName&gt;
///   XS TEMPLATE WIDTH &lt;Left&gt; &lt;Right&gt; SLOPE &lt;LeftH:V&gt; &lt;RightH:V&gt;
///   XS SHOT &lt;Station&gt; &lt;Offset&gt; &lt;Elevation&gt;
///   XS COMPUTE &lt;Interval&gt;
///   XS END
/// </summary>
public class XsCommand : ICommand
{
    public string Name => "XS";
    public string Description => "Cross-section definition. XS BEG <Algn> | XS TEMPLATE WIDTH <L> <R> SLOPE <L> <R> | XS SHOT <Sta> <Off> <Elev> | XS COMPUTE <Interval> | XS END";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: XS requires a subcommand: BEG, TEMPLATE, SHOT, COMPUTE, or END.");
            return Task.CompletedTask;
        }
        string sub = args[1].ToUpperInvariant();

        switch (sub)
        {
            case "BEG":
            {
                if (args.Length < 3) { context.Log("Error: XS BEG requires an alignment name."); return Task.CompletedTask; }
                string name = args[2];
                var algn = context.GetAlignment(name);
                if (algn == null) { context.Log($"Error: Alignment '{name}' not found. Create it with ALGN BEG first."); return Task.CompletedTask; }

                context.XsAlignmentName   = name;
                context.XsGroundShots     = new();
                context.XsTemplateWidthL  = 12.0;
                context.XsTemplateWidthR  = 12.0;
                context.XsForeslopeL      = 2.0;
                context.XsForeslopeR      = 2.0;
                context.Log($"[XS] Opened cross-section session for alignment '{name}'.");
                break;
            }

            case "TEMPLATE":
            {
                // XS TEMPLATE WIDTH <L> <R> SLOPE <L> <R>
                if (args.Length < 8)
                {
                    context.Log("Error: XS TEMPLATE WIDTH <L> <R> SLOPE <L> <R>");
                    return Task.CompletedTask;
                }
                if (!double.TryParse(args[3], out double wl) ||
                    !double.TryParse(args[4], out double wr) ||
                    !double.TryParse(args[6], out double sl) ||
                    !double.TryParse(args[7], out double sr))
                {
                    context.Log("Error: XS TEMPLATE — invalid numeric values.");
                    return Task.CompletedTask;
                }
                context.XsTemplateWidthL = wl;
                context.XsTemplateWidthR = wr;
                context.XsForeslopeL     = sl;
                context.XsForeslopeR     = sr;
                context.Log($"[XS] Template: Width L={wl} R={wr}  Foreslope L={sl}:1 R={sr}:1");
                break;
            }

            case "SHOT":
            {
                // XS SHOT <Station> <Offset> <Elevation>
                if (args.Length < 5)
                {
                    context.Log("Error: XS SHOT <Station> <Offset> <Elevation>");
                    return Task.CompletedTask;
                }
                double sta  = double.TryParse(args[2].Replace("+", ""), out double s) ? s : 0;
                if (!double.TryParse(args[3], out double offset) ||
                    !double.TryParse(args[4], out double elev))
                {
                    context.Log("Error: XS SHOT — invalid numeric values.");
                    return Task.CompletedTask;
                }
                context.XsGroundShots ??= new();
                context.XsGroundShots.Add((sta, offset, elev));
                break;
            }

            case "COMPUTE":
            {
                if (string.IsNullOrEmpty(context.XsAlignmentName))
                {
                    context.Log("Error: XS COMPUTE — no open XS session. Use XS BEG first.");
                    return Task.CompletedTask;
                }

                double interval = 50.0;
                if (args.Length >= 3) double.TryParse(args[2], out interval);

                var algn = context.GetAlignment(context.XsAlignmentName);
                if (algn == null) { context.Log($"Error: Alignment '{context.XsAlignmentName}' not found."); return Task.CompletedTask; }

                // Get FG and EG profiles if available
                Profile? fgProfile = algn.Profiles.FirstOrDefault(p => p.ProfileType.Equals("FG", StringComparison.OrdinalIgnoreCase));
                Profile? egProfile = algn.Profiles.FirstOrDefault(p => p.ProfileType.Equals("EG", StringComparison.OrdinalIgnoreCase));

                var staPts = StationingEngine.StationAlignment(algn, interval, fgProfile, egProfile);
                var sections = StationingEngine.BuildCrossSections(
                    algn, staPts,
                    context.XsGroundShots ?? new(),
                    context.XsTemplateWidthL, context.XsTemplateWidthR,
                    context.XsForeslopeL,     context.XsForeslopeR);

                // Store in context
                context.CrossSections = sections;

                // Output summary report
                string report = StationingEngine.GenerateCutFillReport(sections);
                foreach (var line in report.Split('\n'))
                    context.Log(line.TrimEnd());

                context.Log($"[XS] Computed {sections.Count} cross-sections at {interval} ft intervals.");
                break;
            }

            case "END":
                context.XsAlignmentName = null;
                context.Log("[XS] Cross-section session closed.");
                break;

            default:
                context.Log($"Error: Unknown XS subcommand '{sub}'. Valid: BEG, TEMPLATE, SHOT, COMPUTE, END.");
                break;
        }

        return Task.CompletedTask;
    }
}
