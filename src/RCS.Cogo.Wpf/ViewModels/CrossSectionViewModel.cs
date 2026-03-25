using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using RCS.Alignments.Core;

namespace RCS.Cogo.Wpf.ViewModels;

// ─────────────────────────────────────────────────────────────────────────────
// Drawing primitives the Canvas binds to
// ─────────────────────────────────────────────────────────────────────────────

public class XsPolylineItem
{
    public PointCollection Points { get; set; } = new();
    public Brush           Stroke { get; set; } = Brushes.White;
    public double          Thickness { get; set; } = 1.5;
    public DoubleCollection? DashArray { get; set; }
}

public class XsFillItem
{
    public PointCollection Points { get; set; } = new();
    public Brush           Fill   { get; set; } = Brushes.Red;
    public double          Opacity { get; set; } = 0.35;
}

public class XsLabelItem
{
    public double  X    { get; set; }
    public double  Y    { get; set; }
    public string  Text { get; set; } = string.Empty;
    public Brush   Color { get; set; } = Brushes.White;
    public double  FontSize { get; set; } = 10;
    public bool    Bold { get; set; }
    public double  RotationAngle { get; set; }
}

public class XsTickItem
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public Brush  Stroke { get; set; } = Brushes.Gray;
}

// ─────────────────────────────────────────────────────────────────────────────
// ViewModel
// ─────────────────────────────────────────────────────────────────────────────

public class CrossSectionViewModel : INotifyPropertyChanged
{
    // ── State ──────────────────────────────────────────────────────────────
    private readonly List<CrossSection> _sections;
    private int    _currentIndex;
    private double _canvasWidth  = 900;
    private double _canvasHeight = 400;

    // ── Canvas outputs ─────────────────────────────────────────────────────
    public ObservableCollection<XsPolylineItem> Polylines { get; } = new();
    public ObservableCollection<XsFillItem>     FillAreas { get; } = new();
    public ObservableCollection<XsLabelItem>    Labels    { get; } = new();
    public ObservableCollection<XsTickItem>     Ticks     { get; } = new();

    // ── Navigation ─────────────────────────────────────────────────────────
    public string StationLabel =>
        _sections.Count > 0 ? _sections[_currentIndex].StationLabel : "—";

    public string CutFillLabel
    {
        get
        {
            if (_sections.Count == 0) return "";
            var xs = _sections[_currentIndex];
            double cf = xs.CutFill;
            if (Math.Abs(cf) < 0.01) return "AT GRADE";
            return cf < 0 ? $"CUT  {Math.Abs(cf):F3} ft" : $"FILL  {cf:F3} ft";
        }
    }

    public Brush CutFillBrush
    {
        get
        {
            if (_sections.Count == 0) return Brushes.Gray;
            double cf = _sections[_currentIndex].CutFill;
            if (Math.Abs(cf) < 0.01) return Brushes.LightGreen;
            return cf < 0 ? new SolidColorBrush(Color.FromRgb(255, 80, 80))
                          : new SolidColorBrush(Color.FromRgb(80, 180, 80));
        }
    }

    public int  CurrentIndex { get => _currentIndex; }
    public int  TotalSections => _sections.Count;
    public bool CanPrev => _currentIndex > 0;
    public bool CanNext => _currentIndex < _sections.Count - 1;

    public string SectionCountLabel =>
        _sections.Count > 0 ? $"Section {_currentIndex + 1} of {_sections.Count}" : "No sections";

    // ── ComboBox item list ─────────────────────────────────────────────────
    public List<string> AllStationLabels =>
        _sections.Select(s => s.StationLabel).ToList();

    public int SelectedStationIndex
    {
        get => _currentIndex;
        set { if (value >= 0 && value < _sections.Count) { _currentIndex = value; Refresh(); } }
    }

    // ── Canvas size (set from code-behind after layout) ────────────────────
    public double CanvasWidth  { get => _canvasWidth;  set { _canvasWidth  = value; Refresh(); } }
    public double CanvasHeight { get => _canvasHeight; set { _canvasHeight = value; Refresh(); } }

    // ── Constructor ────────────────────────────────────────────────────────
    public CrossSectionViewModel(List<CrossSection> sections)
    {
        _sections = sections ?? new List<CrossSection>();
        _currentIndex = 0;
        Refresh();
    }

    // ── Navigation Commands ────────────────────────────────────────────────
    public void GoNext()
    {
        if (CanNext) { _currentIndex++; Refresh(); }
    }

    public void GoPrev()
    {
        if (CanPrev) { _currentIndex--; Refresh(); }
    }

    // ── Main render engine ─────────────────────────────────────────────────
    public void Refresh()
    {
        Polylines.Clear();
        FillAreas.Clear();
        Labels.Clear();
        Ticks.Clear();

        if (_sections.Count == 0)
        {
            OnPropertyChanged(nameof(StationLabel));
            OnPropertyChanged(nameof(CutFillLabel));
            OnPropertyChanged(nameof(CutFillBrush));
            OnPropertyChanged(nameof(CanPrev));
            OnPropertyChanged(nameof(CanNext));
            OnPropertyChanged(nameof(SectionCountLabel));
            OnPropertyChanged(nameof(SelectedStationIndex));
            return;
        }

        var xs = _sections[_currentIndex];
        RenderSection(xs);

        OnPropertyChanged(nameof(StationLabel));
        OnPropertyChanged(nameof(CutFillLabel));
        OnPropertyChanged(nameof(CutFillBrush));
        OnPropertyChanged(nameof(CanPrev));
        OnPropertyChanged(nameof(CanNext));
        OnPropertyChanged(nameof(SectionCountLabel));
        OnPropertyChanged(nameof(SelectedStationIndex));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core rendering  
    //
    // Layout:
    //   Left margin  = 50px (Y-axis labels)
    //   Right margin = 20px
    //   Top margin   = 30px
    //   Bottom margin= 50px (X-axis / offset labels)
    //   Plot area    = (CanvasWidth-70) × (CanvasHeight-80)
    //
    //   X-axis = offset from CL (left negative, right positive)
    //   Y-axis = elevation (ground and design)
    // ─────────────────────────────────────────────────────────────────────────
    private void RenderSection(CrossSection xs)
    {
        const double leftMargin   = 60;
        const double rightMargin  = 20;
        const double topMargin    = 40;
        const double bottomMargin = 55;

        double plotW = _canvasWidth  - leftMargin - rightMargin;
        double plotH = _canvasHeight - topMargin  - bottomMargin;

        // ── Determine offset range ───────────────────────────────────────
        double maxHalfWidth = Math.Max(xs.TemplateWidthLeft, xs.TemplateWidthRight) * 2.5;
        if (xs.Shots.Count > 0)
        {
            double shotMax = xs.Shots.Max(s => Math.Abs(s.Offset));
            maxHalfWidth = Math.Max(maxHalfWidth, shotMax * 1.15);
        }
        maxHalfWidth = Math.Max(maxHalfWidth, 30);

        double offMin = -maxHalfWidth;
        double offMax =  maxHalfWidth;
        double offRange = offMax - offMin;

        // ── Determine elevation range ────────────────────────────────────
        var allElevs = new List<double> { xs.FGElevation, xs.EGElevationCL };
        foreach (var s in xs.Shots) allElevs.Add(s.EGElevation);
        // Add FG edge elevations
        allElevs.Add(xs.FGElevation); // flat template assumption

        double elevMin = allElevs.Min() - 1.5;
        double elevMax = allElevs.Max() + 2.0;
        double elevRange = elevMax - elevMin;
        if (elevRange < 2) { elevMin -= 1; elevRange = elevMax - elevMin; }

        // ── Helper transforms ────────────────────────────────────────────
        double ToCanvasX(double offset)   => leftMargin + (offset - offMin) / offRange * plotW;
        double ToCanvasY(double elevation) => topMargin  + (1.0 - (elevation - elevMin) / elevRange) * plotH;

        // ── Grid lines ───────────────────────────────────────────────────
        DrawGrid(xs, offMin, offMax, elevMin, elevMax, ToCanvasX, ToCanvasY, plotH, topMargin, bottomMargin);

        // ── EG polyline (existing ground) ────────────────────────────────
        DrawEgLine(xs, ToCanvasX, ToCanvasY);

        // ── FG template polyline (finished grade road shape) ─────────────
        DrawFgTemplate(xs, ToCanvasX, ToCanvasY, offMax);

        // ── Cut/Fill shading ─────────────────────────────────────────────
        DrawCutFillShading(xs, ToCanvasX, ToCanvasY, offMin, offMax);

        // ── Centerline ───────────────────────────────────────────────────
        double clX = ToCanvasX(0);
        Ticks.Add(new XsTickItem { X1=clX, Y1=topMargin, X2=clX, Y2=topMargin+plotH,
            Stroke = new SolidColorBrush(Color.FromArgb(100,255,255,0)) });
        Labels.Add(new XsLabelItem { X=clX-8, Y=topMargin-18, Text="CL", Color=Brushes.Yellow, FontSize=10, Bold=true });

        // ── Road edge ticks ──────────────────────────────────────────────
        double lEdgeX = ToCanvasX(-xs.TemplateWidthLeft);
        double rEdgeX = ToCanvasX( xs.TemplateWidthRight);
        double edgeY1 = ToCanvasY(xs.FGElevation);
        double edgeY0 = edgeY1 - 10;
        Ticks.Add(new XsTickItem { X1=lEdgeX, Y1=edgeY0, X2=lEdgeX, Y2=edgeY1+6, Stroke=Brushes.Cyan });
        Ticks.Add(new XsTickItem { X1=rEdgeX, Y1=edgeY0, X2=rEdgeX, Y2=edgeY1+6, Stroke=Brushes.Cyan });

        // ── Offset axis labels ───────────────────────────────────────────
        for (double off = Math.Ceiling(offMin / 10) * 10; off <= offMax; off += 10)
        {
            double x = ToCanvasX(off);
            double y = topMargin + plotH;
            Ticks.Add(new XsTickItem { X1=x, Y1=y, X2=x, Y2=y+5, Stroke=Brushes.Gray });
            Labels.Add(new XsLabelItem { X=x-12, Y=y+7, Text=$"{off:+0;-0;0}", Color=Brushes.Gray, FontSize=9 });
        }
        Labels.Add(new XsLabelItem { X=_canvasWidth/2-30, Y=_canvasHeight-14, Text="Offset (ft)", Color=Brushes.Gray, FontSize=10 });

        // ── Elevation axis labels ────────────────────────────────────────
        double elvStep = elevRange > 10 ? 2 : 1;
        for (double elv = Math.Ceiling(elevMin / elvStep) * elvStep; elv <= elevMax; elv += elvStep)
        {
            double y = ToCanvasY(elv);
            Ticks.Add(new XsTickItem { X1=leftMargin-5, Y1=y, X2=leftMargin, Y2=y, Stroke=Brushes.Gray });
            Labels.Add(new XsLabelItem { X=2, Y=y-8, Text=$"{elv:F0}", Color=Brushes.Gray, FontSize=9 });
        }
        Labels.Add(new XsLabelItem { X=10, Y=topMargin+plotH/2-20, Text="Elev", Color=Brushes.Gray, FontSize=10, RotationAngle=-90 });

        // ── Station + CL elevation annotation ───────────────────────────
        Labels.Add(new XsLabelItem { X=leftMargin+5, Y=8,
            Text=$"STA: {xs.StationLabel}   FG: {xs.FGElevation:F3}   EG: {xs.EGElevationCL:F3}",
            Color=Brushes.White, FontSize=11, Bold=true });

        // ── Cut/fill annotation at CL ────────────────────────────────────
        double cfY = ToCanvasY(xs.FGElevation) - 20;
        string cfStr = Math.Abs(xs.CutFill) < 0.01 ? "0.00"
                     : xs.CutFill < 0 ? $"▼{Math.Abs(xs.CutFill):F2} CUT"
                                      : $"▲{xs.CutFill:F2} FILL";
        Brush cfColor = xs.CutFill < -0.01 ? Brushes.OrangeRed :
                        xs.CutFill >  0.01 ? Brushes.LimeGreen  : Brushes.Yellow;
        Labels.Add(new XsLabelItem { X=clX+4, Y=cfY, Text=cfStr, Color=cfColor, FontSize=10, Bold=true });
    }

    private void DrawGrid(CrossSection xs,
        double offMin, double offMax, double elevMin, double elevMax,
        Func<double,double> toX, Func<double,double> toY,
        double plotH, double topMargin, double bottomMargin)
    {
        double offRange  = offMax - offMin;
        double elevRange = elevMax - elevMin;

        // Horizontal grid (elevation)
        double elvStep = elevRange > 10 ? 2 : 1;
        for (double elv = Math.Ceiling(elevMin / elvStep) * elvStep; elv <= elevMax; elv += elvStep)
        {
            double y = toY(elv);
            var pts = new PointCollection { new Point(60, y), new Point(_canvasWidth - 20, y) };
            Polylines.Add(new XsPolylineItem { Points=pts,
                Stroke=new SolidColorBrush(Color.FromArgb(30,255,255,255)), Thickness=0.5 });
        }

        // Vertical grid (offsets)
        for (double off = Math.Ceiling(offMin / 10) * 10; off <= offMax; off += 10)
        {
            double x = toX(off);
            var pts = new PointCollection { new Point(x, topMargin), new Point(x, topMargin+plotH) };
            Polylines.Add(new XsPolylineItem { Points=pts,
                Stroke=new SolidColorBrush(Color.FromArgb(25,255,255,255)), Thickness=0.5 });
        }
    }

    private void DrawEgLine(CrossSection xs, Func<double,double> toX, Func<double,double> toY)
    {
        if (xs.Shots.Count == 0) return;
        var sorted = xs.Shots.OrderBy(s => s.Offset).ToList();
        var pts = new PointCollection();
        foreach (var shot in sorted)
            pts.Add(new Point(toX(shot.Offset), toY(shot.EGElevation)));

        Polylines.Add(new XsPolylineItem
        {
            Points    = pts,
            Stroke    = new SolidColorBrush(Color.FromRgb(80, 160, 240)),
            Thickness = 2.0,
            DashArray = new DoubleCollection { 6, 3 }
        });

        // EG dots at each shot
        foreach (var shot in sorted)
        {
            Labels.Add(new XsLabelItem
            {
                X = toX(shot.Offset) - 2,
                Y = toY(shot.EGElevation) - 2,
                Text = "●",
                Color = new SolidColorBrush(Color.FromRgb(80, 160, 240)),
                FontSize = 7
            });
        }
    }

    private void DrawFgTemplate(CrossSection xs, Func<double,double> toX, Func<double,double> toY, double offMax)
    {
        // Flat pavement surface from -WidthL to +WidthR at FGElevation
        double fgElev = xs.FGElevation;
        double lEdge  = -xs.TemplateWidthLeft;
        double rEdge  =  xs.TemplateWidthRight;

        // Left foreslope to daylight
        double lDayOff = lEdge - (Math.Abs(xs.EGElevationCL - fgElev) / xs.ForeslopeLeft + xs.TemplateWidthLeft);
        lDayOff = Math.Max(lDayOff, -offMax * 0.9);
        double lDayElev = xs.GetGroundElevAt(lDayOff) ?? (fgElev + (lEdge - lDayOff) / xs.ForeslopeLeft);

        // Right foreslope to daylight
        double rDayOff = rEdge + (Math.Abs(xs.EGElevationCL - fgElev) / xs.ForeslopeRight + xs.TemplateWidthRight);
        rDayOff = Math.Min(rDayOff, offMax * 0.9);
        double rDayElev = xs.GetGroundElevAt(rDayOff) ?? (fgElev + (rDayOff - rEdge) / xs.ForeslopeRight);

        var pts = new PointCollection
        {
            new Point(toX(lDayOff),  toY(lDayElev)),   // left daylight
            new Point(toX(lEdge),    toY(fgElev)),      // left edge of road
            new Point(toX(0),        toY(fgElev)),      // centerline
            new Point(toX(rEdge),    toY(fgElev)),      // right edge of road
            new Point(toX(rDayOff),  toY(rDayElev)),    // right daylight
        };

        Polylines.Add(new XsPolylineItem
        {
            Points    = pts,
            Stroke    = new SolidColorBrush(Color.FromRgb(255, 210, 50)),
            Thickness = 2.5,
        });
    }

    private void DrawCutFillShading(CrossSection xs, Func<double,double> toX, Func<double,double> toY,
        double offMin, double offMax)
    {
        if (xs.Shots.Count < 2) return;

        var shots = xs.Shots.OrderBy(s => s.Offset).ToList();
        double fgElev = xs.FGElevation;

        // Walk shot pairs and shade cut (red) or fill (green) between EG and FG
        for (int i = 0; i < shots.Count - 1; i++)
        {
            var a = shots[i];
            var b = shots[i + 1];

            double egA = a.EGElevation;
            double egB = b.EGElevation;
            double cfA = fgElev - egA;
            double cfB = fgElev - egB;

            // Both cut
            if (cfA <= 0 && cfB <= 0)
            {
                AddFillPolygon(
                    toX(a.Offset), toY(egA),
                    toX(b.Offset), toY(egB),
                    toX(b.Offset), toY(fgElev),
                    toX(a.Offset), toY(fgElev),
                    new SolidColorBrush(Color.FromRgb(220, 60, 60)), 0.30);
            }
            // Both fill
            else if (cfA >= 0 && cfB >= 0)
            {
                AddFillPolygon(
                    toX(a.Offset), toY(fgElev),
                    toX(b.Offset), toY(fgElev),
                    toX(b.Offset), toY(egB),
                    toX(a.Offset), toY(egA),
                    new SolidColorBrush(Color.FromRgb(60, 200, 80)), 0.28);
            }
            // Transition — interpolate zero crossing
            else
            {
                double tZero = Math.Abs(cfA) / (Math.Abs(cfA) + Math.Abs(cfB));
                double zeroOff  = a.Offset  + tZero * (b.Offset  - a.Offset);
                double zeroEg   = egA + tZero * (egB - egA);

                if (cfA < 0) // left=cut, right=fill
                {
                    AddFillPolygon(toX(a.Offset), toY(egA),
                        toX(zeroOff), toY(zeroEg),
                        toX(zeroOff), toY(fgElev),
                        toX(a.Offset), toY(fgElev),
                        new SolidColorBrush(Color.FromRgb(220,60,60)), 0.30);
                    AddFillPolygon(toX(zeroOff), toY(fgElev),
                        toX(b.Offset), toY(fgElev),
                        toX(b.Offset), toY(egB),
                        toX(zeroOff), toY(zeroEg),
                        new SolidColorBrush(Color.FromRgb(60,200,80)), 0.28);
                }
                else // left=fill, right=cut
                {
                    AddFillPolygon(toX(a.Offset), toY(fgElev),
                        toX(zeroOff), toY(fgElev),
                        toX(zeroOff), toY(zeroEg),
                        toX(a.Offset), toY(egA),
                        new SolidColorBrush(Color.FromRgb(60,200,80)), 0.28);
                    AddFillPolygon(toX(zeroOff), toY(egB),
                        toX(b.Offset), toY(egB),
                        toX(b.Offset), toY(fgElev),
                        toX(zeroOff), toY(fgElev),
                        new SolidColorBrush(Color.FromRgb(220,60,60)), 0.30);
                }
            }
        }
    }

    private void AddFillPolygon(double x1, double y1, double x2, double y2,
                                 double x3, double y3, double x4, double y4,
                                 Brush fill, double opacity)
    {
        FillAreas.Add(new XsFillItem
        {
            Points  = new PointCollection { new(x1,y1), new(x2,y2), new(x3,y3), new(x4,y4) },
            Fill    = fill,
            Opacity = opacity
        });
    }

    // ── INotifyPropertyChanged ─────────────────────────────────────────────
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
