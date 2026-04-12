using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RCS.Alignments.Core;
using RCS.Cogo.App.Commands;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;
using Xunit;

namespace RCS.Cogo.Core.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// Minimal in-memory ICogoContext stub (no WPF / DB dependencies)
// Every interface member that RENUMBER does not use is stubbed with a default.
// ─────────────────────────────────────────────────────────────────────────────
internal sealed class StubCogoContext : ICogoContext
{
    private readonly Dictionary<string, (Point3D Pt, string Desc)> _pts =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Figure> _figs = new();
    public readonly  List<string> Messages = new();
    private int _nextPtId = 1;

    // ── Point management ─────────────────────────────────────────────────────
    public void  AddPoint(string id, Point3D pt, string desc = "") => _pts[id] = (pt, desc);
    public Point3D? GetPoint(string id) => _pts.TryGetValue(id, out var v) ? v.Pt : null;
    public bool  RemovePoint(string id) => _pts.Remove(id);
    public bool  DeletePoint(string id) => _pts.Remove(id);   // interface alias
    public bool  RenamePoint(string oldId, string newId)
    {
        if (!_pts.TryGetValue(oldId, out var data)) return false;
        if (_pts.ContainsKey(newId)) return false;
        _pts.Remove(oldId);
        _pts[newId] = data;
        return true;
    }
    public int   GetNextPointId() => _nextPtId++;
    public IEnumerable<(string Id, Point3D Point, string Description)> GetAllPoints()
        => _pts.Select(kv => (kv.Key, kv.Value.Pt, kv.Value.Desc));

    // ── Figure management ─────────────────────────────────────────────────────
    public void    AddFigure(Figure f) => _figs.Add(f);
    public Figure? GetFigure(string name) => _figs.FirstOrDefault(f => f.Name == name);
    public bool    DeleteFigure(string name) { var f = GetFigure(name); if (f != null) _figs.Remove(f); return f != null; }
    public IEnumerable<Figure> GetAllFigures() => _figs;

    // ── Logging ───────────────────────────────────────────────────────────────
    public void Log(string msg) => Messages.Add(msg);
    public void ClearLog()      => Messages.Clear();

    // ── Environment properties (unused by RENUMBER) ──────────────────────────
    public Point3D? CurrentStation  { get; set; }
    public Point3D? CurrentBacksight{ get; set; }
    public bool  TraverseMode       { get; set; }
    public string Units             { get; set; } = "Foot";
    public double Temperature       { get; set; }
    public double Pressure          { get; set; }
    public double ScaleFactor       { get; set; } = 1.0;
    public bool  AtmosCorrection    { get; set; }
    public bool  CurvatureRefraction{ get; set; }
    public bool  AutoPoint          { get; set; }
    public string AngleFormat       { get; set; } = "Right";
    public string VerticalFormat    { get; set; } = "Zenith";
    public string EdmMode           { get; set; } = "Normal";
    public string PrismMode         { get; set; } = "S";
    public double MapCheckClosureTolerance { get; set; } = 0.05;
    public bool  ShowAlignmentLabels        { get; set; }
    public bool  ShowVerticalAlignmentLabels{ get; set; }
    public bool  ShowVPIs                   { get; set; }
    public bool  ShowGradePercent           { get; set; }
    public bool  OutputEnabled               { get; set; } = true;

    // ── Figure / session state ────────────────────────────────────────────────
    public Figure? CurrentFigure { get; set; }
    public (Point3D? Left, Point3D? Right) LastIntersections { get; set; }

    // ── Alignment stubs ───────────────────────────────────────────────────────
    public Alignment? CurrentAlignment { get; set; }
    public Profile?   CurrentProfile   { get; set; }
    public void AddAlignment(Alignment a) { }
    public Alignment? GetAlignment(string name) => null;
    public IEnumerable<Alignment> GetAllAlignments() => [];

    // ── Cross-section session state ───────────────────────────────────────────
    public string? XsAlignmentName { get; set; }
    public List<(double Station, double Offset, double Elevation)>? XsGroundShots { get; set; }
    public double XsTemplateWidthL { get; set; }
    public double XsTemplateWidthR { get; set; }
    public double XsForeslopeL     { get; set; }
    public double XsForeslopeR     { get; set; }
    public List<CrossSection>? CrossSections { get; set; }

    // ── Actions ───────────────────────────────────────────────────────────────
    public System.Action<string, string>? SaveHorizontalAlignmentAction { get; set; }
    public System.Action<string, string>? SaveProfileAlignmentAction    { get; set; }
    public System.Action? SyncPointsAction                              { get; set; }
    public System.Action<IEnumerable<ICommand>>? OpenHelpWindowAction   { get; set; }
    public string? ProjectDirectory { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────
// RENUMBER Command Tests
// ─────────────────────────────────────────────────────────────────────────────
public class RenumberCommandTests
{
    private static StubCogoContext BuildContext(int from, int to)
    {
        var ctx = new StubCogoContext();
        for (int i = from; i <= to; i++)
            ctx.AddPoint(i.ToString(), new Point3D(i * 10.0, i * 5.0, 0), $"PT{i}");
        return ctx;
    }

    private static Task Run(StubCogoContext ctx, string args)
        => new RenumberCommand().ExecuteAsync(args.Split(' '), ctx);

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Renumber_BasicRange_OldIdsGone_NewIdsPresent()
    {
        var ctx = BuildContext(1, 5);
        await Run(ctx, "RENUMBER 1 5 101");

        for (int i = 1;   i <= 5;   i++) Assert.Null(ctx.GetPoint(i.ToString()));
        for (int i = 101; i <= 105; i++) Assert.NotNull(ctx.GetPoint(i.ToString()));
    }

    [Fact]
    public async Task Renumber_CoordinatesPreserved()
    {
        var ctx    = BuildContext(1, 3);
        var orig1  = ctx.GetPoint("1")!;
        await Run(ctx, "RENUMBER 1 3 201");

        var moved = ctx.GetPoint("201")!;
        Assert.InRange(moved.Northing, orig1.Northing - 0.001, orig1.Northing + 0.001);
        Assert.InRange(moved.Easting,  orig1.Easting  - 0.001, orig1.Easting  + 0.001);
    }

    [Fact]
    public async Task Renumber_DescriptionPreserved()
    {
        var ctx = new StubCogoContext();
        ctx.AddPoint("10", new Point3D(100, 200, 0), "IRON PIN");
        await Run(ctx, "RENUMBER 10 10 999");

        Assert.Null(ctx.GetPoint("10"));
        var moved = ctx.GetAllPoints().First(p => p.Id == "999");
        Assert.Equal("IRON PIN", moved.Description);
    }

    [Fact]
    public async Task Renumber_SinglePoint_Works()
    {
        var ctx = BuildContext(42, 42);
        await Run(ctx, "RENUMBER 42 42 1000");
        Assert.Null(ctx.GetPoint("42"));
        Assert.NotNull(ctx.GetPoint("1000"));
    }

    [Fact]
    public async Task Renumber_FigureReferencesUpdated()
    {
        var ctx = BuildContext(1, 3);
        var fig = new Figure("BOUNDARY");
        fig.AddPoint("1"); fig.AddPoint("2"); fig.AddPoint("3");
        ctx.AddFigure(fig);

        await Run(ctx, "RENUMBER 1 3 101");

        Assert.Equal(new[] { "101", "102", "103" }, fig.PointIds.ToArray());
    }

    [Fact]
    public async Task Renumber_PartialFigureRefs_OnlyRenamedOnesUpdate()
    {
        var ctx = BuildContext(1, 5);
        var fig = new Figure("ROW");
        fig.AddPoint("1"); fig.AddPoint("3"); fig.AddPoint("5"); // odd only
        ctx.AddFigure(fig);

        await Run(ctx, "RENUMBER 1 5 201");

        // 1→201, 3→203, 5→205
        Assert.Equal(new[] { "201", "203", "205" }, fig.PointIds.ToArray());
    }

    [Fact]
    public async Task Renumber_CorrectCount_InLog()
    {
        var ctx = BuildContext(10, 15);
        await Run(ctx, "RENUMBER 10 15 100");

        Assert.True(ctx.Messages.Any(l => l.Contains("6")));   // 6 points renamed
    }

    // ── Collision guard ───────────────────────────────────────────────────────

    [Fact]
    public async Task Renumber_CollidesWithExternalPoint_Aborts()
    {
        var ctx = BuildContext(1, 3);
        ctx.AddPoint("102", new Point3D(999, 999, 0), "BLOCKER"); // blocks new slot 102

        await Run(ctx, "RENUMBER 1 3 101");

        // Nothing moved
        Assert.NotNull(ctx.GetPoint("1"));
        Assert.NotNull(ctx.GetPoint("2"));
        Assert.True(ctx.Messages.Any(l => l.Contains("Aborting")));
    }

    // ── Validation errors ─────────────────────────────────────────────────────

    [Fact]
    public async Task Renumber_StartGreaterThanEnd_LogsError()
    {
        var ctx = BuildContext(1, 5);
        await Run(ctx, "RENUMBER 5 1 100");
        Assert.True(ctx.Messages.Any(l => l.Contains("≤")));
        Assert.NotNull(ctx.GetPoint("1")); // unchanged
    }

    [Fact]
    public async Task Renumber_TooFewArgs_LogsUsage()
    {
        var ctx = new StubCogoContext();
        await new RenumberCommand().ExecuteAsync(["RENUMBER", "1"], ctx);
        Assert.True(ctx.Messages.Any(l => l.Contains("Usage")));
    }

    [Fact]
    public async Task Renumber_NoneExist_LogsNothingChanged()
    {
        var ctx = new StubCogoContext(); // empty
        await Run(ctx, "RENUMBER 1 5 100");
        Assert.True(ctx.Messages.Any(l => l.ToLower().Contains("nothing") ||
                                     l.ToLower().Contains("not found") ||
                                     l.ToLower().Contains("none")));
    }

    [Fact]
    public async Task Renumber_NonIntegerArg_LogsError()
    {
        var ctx = new StubCogoContext();
        await new RenumberCommand().ExecuteAsync(["RENUMBER", "A", "5", "100"], ctx);
        Assert.True(ctx.Messages.Any(l => l.ToLower().Contains("integer") || l.ToLower().Contains("usage")));
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Closure / Area Calculation Tests
// These mirror the Shoelace + Inverse logic inside WriteFigureClosureTables.
// ─────────────────────────────────────────────────────────────────────────────
public class ClosureAndAreaTests
{
    private const double Tol = 0.001;

    private static double ShoelaceArea(IList<Point3D> pts)
    {
        double sum = 0;
        for (int i = 0; i < pts.Count; i++)
        {
            var a = pts[i];
            var b = pts[(i + 1) % pts.Count];
            sum += a.Easting * b.Northing - b.Easting * a.Northing;
        }
        return Math.Abs(sum) * 0.5;
    }

    private static double Perimeter(IList<Point3D> pts)
    {
        double p = 0;
        for (int i = 0; i < pts.Count - 1; i++)
            p += GeometryEngine.Inverse(pts[i], pts[i + 1]).Distance;
        return p;
    }

    /// <summary>100×100 ft square: area=10,000 sq ft. Perimeter() counts N-1=3 sides.
    /// The closing leg is the 4th side (also 100 ft). Closure = 0 (last pt→first pt = 0).</summary>
    [Fact]
    public void Square100x100_CorrectAreaPerimeterClosure()
    {
        var pts = new List<Point3D>
        {
            new(10000,       10000,       0),
            new(10100,       10000,       0),
            new(10100,       10100,       0),
            new(10000,       10100,       0),
        };

        // Area: full shoelace (all 4 sides implicitly)
        Assert.InRange(ShoelaceArea(pts), 10000 - Tol, 10000 + Tol);

        // Perimeter() sums N-1=3 segments (300 ft); the closing leg is pts[3]→pts[0] = 100 ft N
        double openPerim = Perimeter(pts);
        var    closeLeg  = GeometryEngine.Inverse(pts[^1], pts[0]);
        Assert.InRange(openPerim + closeLeg.Distance, 400 - Tol, 400 + Tol);

        // The 4-point list is open: pts[3]=(10000,10100) → pts[0]=(10000,10000) = 100 ft
        // openPerim=300, closeLeg=100 → total = 400  ✓
        Assert.InRange(closeLeg.Distance, 100 - Tol, 100 + Tol);
    }

    /// <summary>3-4-5 right triangle: area=6. The two legs are 3+4=7; hypotenuse is 5.
    /// Perimeter() sums N-1=2 sides (the two legs = 3+4 = 7 ft).</summary>
    [Fact]
    public void Triangle3_4_5_CorrectArea()
    {
        var pts = new List<Point3D>
        {
            new(10000, 10000, 0),   // start (hypotenuse end)
            new(10003, 10000, 0),   // N +3 → Leg1 = 3 ft (right angle here)
            new(10003, 10004, 0),   // E +4 → Leg2 = 4 ft
        };

        // Shoelace area: 0.5 * 3 * 4 = 6
        Assert.InRange(ShoelaceArea(pts), 6.0 - Tol, 6.0 + Tol);

        // Perimeter() counts 2 open legs; add closing hypotenuse separately.
        // Leg1: pts[0]→pts[1] = ΔN=3, ΔE=0 → 3 ft
        // Leg2: pts[1]→pts[2] = ΔN=0, ΔE=4 → 4 ft  (right angle at pts[1])
        // Hypotenuse (closing): pts[0]→pts[2] → 5 ft
        double openLegs   = Perimeter(pts);
        var    hypotenuse = GeometryEngine.Inverse(pts[^1], pts[0]);
        Assert.InRange(openLegs,              7.0  - Tol, 7.0  + Tol);   // 3+4
        Assert.InRange(hypotenuse.Distance,   5.0  - Tol, 5.0  + Tol);   // 3-4-5
        Assert.InRange(openLegs + hypotenuse.Distance, 12.0 - Tol, 12.0 + Tol);
    }

    /// <summary>One-acre square (≈208.71 ft): acreage must equal 1.000 ± 0.0005.</summary>
    [Fact]
    public void OneAcreSquare_CorrectAcreage()
    {
        const double side = 208.7103; // feet in a 1-acre square
        var pts = new List<Point3D>
        {
            new(10000,        10000,        0),
            new(10000 + side, 10000,        0),
            new(10000 + side, 10000 + side, 0),
            new(10000,        10000 + side, 0),
        };

        double acres = ShoelaceArea(pts) / 43560.0;
        Assert.InRange(acres, 1.0 - 0.0005, 1.0 + 0.0005);
    }

    /// <summary>Open L-shape: last point ≠ first → non-zero closure distance.</summary>
    [Fact]
    public void OpenPolyline_HasNonZeroClosure()
    {
        var pts = new List<Point3D>
        {
            new(10000, 10000, 0),
            new(10100, 10000, 0),
            new(10100, 10050, 0),   // deliberately NOT at start
        };

        var close = GeometryEngine.Inverse(pts[^1], pts[0]);
        Assert.True(close.Distance > 0.01);
    }

    /// <summary>Precision ratio: 400 ft perimeter / 0.04 ft error → 1 : 10,000.</summary>
    [Fact]
    public void PrecisionRatio_400ft_0p04Error_Is10000()
    {
        double ratio = 400.0 / 0.04;
        Assert.InRange(ratio, 9999, 10001);
    }

    /// <summary>
    /// A truly closed polygon (last point == first point): closure distance = 0.
    /// Mirrors WriteFigureClosureTables isClosed threshold of ≤ 0.02.
    /// </summary>
    [Fact]
    public void ClosedSquare_BelowClosureThreshold()
    {
        // Create a square where the last point IS the first point (truly closed)
        var start = new Point3D(10000, 10000, 0);
        var pts = new List<Point3D>
        {
            start,
            new(10050, 10000, 0),
            new(10050, 10050, 0),
            new(10000, 10050, 0),
            start,   // explicitly close back to start
        };

        // The last element IS the first point, so Inverse distance = 0
        var closure = GeometryEngine.Inverse(pts[^1], pts[0]);
        Assert.True(closure.Distance <= 0.02,
            $"Expected closure ≤ 0.02, got {closure.Distance:F6}");
    }
}
