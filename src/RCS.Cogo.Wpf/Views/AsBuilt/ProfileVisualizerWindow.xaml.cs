using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using RCS.Piping.Core.Models;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

/// <summary>
/// Interactive 2-D sewer profile visualiser.
///
/// Features:
///   • Pipe centerline + crown/invert barrel drawn to scale
///   • Existing ground surface line interpolated from PointRows
///   • X-crossings detected and annotated with clearance
///   • Vertical exaggeration toggling (1× / 2× / 5× / 10×)
///   • Y-axis elevation ticks, X-axis stationing labels
///   • Draggable invert nodes (live slope update)
///   • Crosshair mouse read-out (station & elevation)
///   • PNG export via RenderTargetBitmap
/// </summary>
public partial class ProfileVisualizerWindow : Window
{
    // ── State fields ───────────────────────────────────────────────────────────
    private readonly AsBuiltJob          _job;
    private          PipeRun?            _targetRun;
    private          bool                _showAllRuns = false;

    private static readonly double[] ExagSteps = { 1.0, 2.0, 5.0, 10.0 };
    private int    _exagIdx  = 2;   // Default: 5×
    private double VertExag => ExagSteps[_exagIdx];

    // Computed layout constants (recalculated on every draw)
    private double _baseZ;
    private double _topZ;
    private double _totalLen;
    private double _xScale;
    private double _yScale;
    private double _canvasH;
    private double _canvasW;

    // ── Constructor ────────────────────────────────────────────────────────────
    public ProfileVisualizerWindow(AsBuiltJob job, PipeRun? targetRun = null)
    {
        InitializeComponent();
        _job       = job;
        _targetRun = targetRun;

        if (_targetRun != null)
            SetTitles(_targetRun);
        else
        {
            TxtTitle.Text    = "All Pipe Runs — Network Profile";
            TxtSubtitle.Text = $"{job.Network.Runs.Count} runs  ·  {job.Identity.JobNumber}";
        }

        Loaded += (_, _) => Draw();
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Update the highlighted run without reopening the window.</summary>
    public void FocusRun(PipeRun run)
    {
        _targetRun   = run;
        _showAllRuns = false;
        SetTitles(run);
        Draw();
    }

    // ── Core drawing entry ─────────────────────────────────────────────────────
    private void Draw()
    {
        ProfileCanvas.Children.Clear();
        YAxisCanvas.Children.Clear();
        XAxisCanvas.Children.Clear();

        _canvasW = ProfileCanvas.ActualWidth;
        _canvasH = ProfileCanvas.ActualHeight;
        if (_canvasW < 10 || _canvasH < 10) return;

        // Collect all runs to draw
        var runs = _showAllRuns || _targetRun == null
            ? _job.Network.Runs.Values.ToList()
            : new List<PipeRun> { _targetRun };

        if (runs.Count == 0)
        {
            DrawNoData("No runs to display.");
            return;
        }

        // ── 1. Compute global elevation window ────────────────────────────────
        ComputeElevationWindow(runs, out _baseZ, out _topZ, out _totalLen);
        double zRange = (_topZ - _baseZ) * VertExag;
        if (zRange < 0.5) zRange = 10;

        _xScale = _canvasW / Math.Max(_totalLen, 10);
        _yScale = _canvasH / zRange;

        // ── 2. Draw grid lines ────────────────────────────────────────────────
        DrawGrid();

        // ── 3. Draw existing ground surface ───────────────────────────────────
        DrawGroundSurface(runs);

        // ── 4. Draw each run (invert barrel + crown) ──────────────────────────
        double stationOffset = 0;
        foreach (var run in runs)
        {
            bool isTarget = run == _targetRun;
            DrawRun(run, stationOffset, isTarget);
            stationOffset += run.ComputedLength > 0 ? run.ComputedLength : 10;
        }

        // ── 5. Draw crossing utility pipes ────────────────────────────────────
        DrawCrossings();

        // ── 6. Axis labels ────────────────────────────────────────────────────
        DrawYAxisLabels();
        DrawXAxisLabels(runs);

        // ── 7. Status bar ─────────────────────────────────────────────────────
        double totalLength = runs.Sum(r => r.ComputedLength);
        double minSlope    = runs.Where(r => r.SlopePercent > 0).Select(r => r.SlopePercent).DefaultIfEmpty(0).Min();
        double maxSlope    = runs.Max(r => r.SlopePercent);
        TxtStatus.Text = $"Total Length: {totalLength:F2} ft   |   " +
                         $"Slope range: {minSlope:F3}%–{maxSlope:F3}%   |   " +
                         $"Exaggeration: {VertExag}×   |   Runs: {runs.Count}";
    }

    // ── Elevation window ───────────────────────────────────────────────────────
    private void ComputeElevationWindow(IList<PipeRun> runs, out double baseZ, out double topZ, out double totalLen)
    {
        baseZ    = double.MaxValue;
        topZ     = double.MinValue;
        totalLen = 0;

        foreach (var run in runs)
        {
            double s = run.InvertStart ?? GetPointElevation(run.FromPointId, 0);
            double e = run.InvertEnd   ?? GetPointElevation(run.ToPointId,   0);
            double g1 = GetPointElevation(run.FromPointId, s);
            double g2 = GetPointElevation(run.ToPointId,   e);
            double crown1 = s + run.Diameter / 12.0;
            double crown2 = e + run.Diameter / 12.0;

            baseZ    = Math.Min(baseZ, Math.Min(s, e) - 1.5);
            topZ     = Math.Max(topZ,  Math.Max(Math.Max(g1, g2), Math.Max(crown1, crown2)) + 2.0);
            totalLen += run.ComputedLength > 0 ? run.ComputedLength : 10;
        }

        if (baseZ == double.MaxValue) { baseZ = 0; topZ = 20; }
    }

    // ── Grid lines ─────────────────────────────────────────────────────────────
    private void DrawGrid()
    {
        // Horizontal grid for every nice elevation interval
        double zRange = (_topZ - _baseZ) * VertExag;
        double interval = NiceInterval(zRange / 6.0);

        double startTick = Math.Ceiling(_baseZ / interval) * interval;
        for (double z = startTick; z <= _topZ + 0.01; z += interval)
        {
            double y = GetY(z);
            if (y < 0 || y > _canvasH) continue;
            var gl = new Line { X1 = 0, Y1 = y, X2 = _canvasW, Y2 = y,
                Stroke = new SolidColorBrush(Color.FromArgb(40, 100, 100, 150)),
                StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 4, 4 } };
            ProfileCanvas.Children.Add(gl);
        }
    }

    // ── Ground surface ─────────────────────────────────────────────────────────
    private void DrawGroundSurface(IList<PipeRun> runs)
    {
        var pts = new PointCollection();
        double station = 0;
        bool first = true;

        foreach (var run in runs)
        {
            double len = run.ComputedLength > 0 ? run.ComputedLength : 10;
            double g1  = GetPointElevation(run.FromPointId, run.InvertStart ?? 0);
            double g2  = GetPointElevation(run.ToPointId,   run.InvertEnd   ?? 0);

            if (first)
            {
                pts.Add(new Point(station * _xScale, GetY(g1)));
                first = false;
            }
            pts.Add(new Point((station + len) * _xScale, GetY(g2)));
            station += len;
        }

        if (pts.Count < 2) return;
        var poly = new Polyline {
            Points = pts,
            Stroke = new SolidColorBrush(Color.FromRgb(116, 199, 236)),   // Sapphire
            StrokeThickness = 1.5,
            StrokeDashArray = new DoubleCollection { 6, 3 }
        };
        ProfileCanvas.Children.Add(poly);
    }

    // ── Single run drawing ─────────────────────────────────────────────────────
    private void DrawRun(PipeRun run, double stationOffset, bool isHighlighted)
    {
        double len     = run.ComputedLength > 0 ? run.ComputedLength : 10;
        double startZ  = run.InvertStart ?? GetPointElevation(run.FromPointId, 0);
        double endZ    = run.InvertEnd   ?? GetPointElevation(run.ToPointId,   0);
        double crowns  = startZ + run.Diameter / 12.0;
        double crowne  = endZ   + run.Diameter / 12.0;

        double x1 = stationOffset * _xScale;
        double x2 = (stationOffset + len) * _xScale;

        // Colors
        var invertColor = isHighlighted
            ? Color.FromRgb(46, 204, 113)   // Emerald green
            : Color.FromRgb(49, 116, 84);   // Muted green
        var crownColor  = isHighlighted
            ? Color.FromRgb(166, 227, 161)  // Light green
            : Color.FromRgb(88, 130, 100);  // Muted

        // Invert (flow line)
        var invertLine = new Line {
            X1 = x1, Y1 = GetY(startZ),
            X2 = x2, Y2 = GetY(endZ),
            Stroke = new SolidColorBrush(invertColor),
            StrokeThickness = isHighlighted ? 2.5 : 1.5,
            ToolTip = $"Invert  {startZ:F2}' → {endZ:F2}'\nSlope: {run.SlopePercent:F3}%\nL={len:F2}'"
        };
        ProfileCanvas.Children.Add(invertLine);

        // Crown line
        var crownLine = new Line {
            X1 = x1, Y1 = GetY(crowns),
            X2 = x2, Y2 = GetY(crowne),
            Stroke = new SolidColorBrush(crownColor),
            StrokeThickness = isHighlighted ? 1.5 : 1,
            StrokeDashArray = isHighlighted ? null : new DoubleCollection { 4, 2 }
        };
        ProfileCanvas.Children.Add(crownLine);

        // Pipe barrel fill (translucent fill between invert and crown)
        if (isHighlighted)
        {
            var barrel = new Polygon {
                Fill = new SolidColorBrush(Color.FromArgb(35, 46, 204, 113)),
                Stroke = Brushes.Transparent,
                Points = new PointCollection {
                    new(x1, GetY(startZ)),
                    new(x2, GetY(endZ)),
                    new(x2, GetY(crowne)),
                    new(x1, GetY(crowns))
                }
            };
            ProfileCanvas.Children.Add(barrel);
        }

        // Structure symbols (manhole rectangles) at start
        DrawManholeSymbol(x1, GetY(startZ), GetY(crowns), run.FromPointId, run.Type);

        // Bearing/slope label on run
        if (isHighlighted && len > 20)
        {
            double midX = (x1 + x2) / 2;
            double midY = GetY((startZ + endZ) / 2) - 14;
            var lbl = new TextBlock {
                Text = $"{run.Id}  S={run.SlopePercent:F3}%  L={len:F1}'",
                Foreground = new SolidColorBrush(Color.FromRgb(166, 227, 161)),
                FontSize = 9, FontFamily = new FontFamily("Consolas"),
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 15, 23))
            };
            Canvas.SetLeft(lbl, midX - 50);
            Canvas.SetTop(lbl, midY);
            ProfileCanvas.Children.Add(lbl);
        }

        // Draggable invert nodes (highlighted run only)
        if (isHighlighted)
        {
            double p1Elev = startZ;
            double p2Elev = endZ;
            AddDraggableNode(run, true,  x1, GetY(startZ), p1Elev, stationOffset, len);
            AddDraggableNode(run, false, x2, GetY(endZ),   p2Elev, stationOffset, len);
        }
    }

    // ── Manhole structure symbol ───────────────────────────────────────────────
    private void DrawManholeSymbol(double x, double invertY, double crownY, string pointId, string pipeType)
    {
        double h = Math.Max(Math.Abs(crownY - invertY), 8);
        var rect = new Rectangle {
            Width = 8, Height = h + 6,
            Fill   = new SolidColorBrush(Color.FromArgb(80, 100, 100, 200)),
            Stroke = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
            StrokeThickness = 1,
            ToolTip = $"Structure: {pointId}\nType: {pipeType}"
        };
        Canvas.SetLeft(rect, x - 4);
        Canvas.SetTop(rect, Math.Min(crownY, invertY) - 3);
        ProfileCanvas.Children.Add(rect);

        var pid = new TextBlock {
            Text = pointId, FontSize = 8.5,
            Foreground = new SolidColorBrush(Color.FromRgb(137, 180, 250)),
            FontFamily = new FontFamily("Consolas"),
            RenderTransform = new RotateTransform(-90),
            RenderTransformOrigin = new Point(0, 0)
        };
        Canvas.SetLeft(pid, x + 4);
        Canvas.SetTop(pid, Math.Min(crownY, invertY) + 20);
        ProfileCanvas.Children.Add(pid);
    }

    // ── Crossing utilities ─────────────────────────────────────────────────────
    private void DrawCrossings()
    {
        if (_targetRun == null) return;
        var run = _targetRun;
        var p1  = _job.PointRows.FirstOrDefault(r => r.PointId == run.FromPointId);
        var p2  = _job.PointRows.FirstOrDefault(r => r.PointId == run.ToPointId);
        if (p1 == null || p2 == null) return;

        double len = run.ComputedLength > 0 ? run.ComputedLength : 10;

        foreach (var r2 in _job.Network.Runs.Values)
        {
            if (r2.Id == run.Id) continue;
            var p3 = _job.PointRows.FirstOrDefault(r => r.PointId == r2.FromPointId);
            var p4 = _job.PointRows.FirstOrDefault(r => r.PointId == r2.ToPointId);
            if (p3 == null || p4 == null) continue;

            double denom = (p4.Northing - p3.Northing) * (p2.Easting - p1.Easting)
                         - (p4.Easting  - p3.Easting)  * (p2.Northing - p1.Northing);
            if (Math.Abs(denom) < 1e-9) continue;

            double uA = ((p4.Easting  - p3.Easting)  * (p1.Northing - p3.Northing)
                       - (p4.Northing - p3.Northing)  * (p1.Easting  - p3.Easting)) / denom;
            double uB = ((p2.Easting  - p1.Easting)  * (p1.Northing - p3.Northing)
                       - (p2.Northing - p1.Northing)  * (p1.Easting  - p3.Easting)) / denom;

            if (uA < 0 || uA > 1 || uB < 0 || uB > 1) continue;

            double z2Cross   = (r2.InvertStart ?? p3.Elevation) + uB * ((r2.InvertEnd ?? p4.Elevation) - (r2.InvertStart ?? p3.Elevation));
            double z1AtCross = (run.InvertStart ?? p1.Elevation) + uA * ((run.InvertEnd ?? p2.Elevation) - (run.InvertStart ?? p1.Elevation));
            double clearance = z2Cross - z1AtCross;

            double xPlot = uA * len * _xScale;
            double yPlot = GetY(z2Cross);

            // Cross pipe symbol — vertical dash at crossing
            var xLine = new Line {
                X1 = xPlot, Y1 = yPlot - 14,
                X2 = xPlot, Y2 = yPlot + 14,
                Stroke = new SolidColorBrush(Color.FromRgb(243, 139, 168)),  // Red
                StrokeThickness = 2
            };
            ProfileCanvas.Children.Add(xLine);

            var dot = new Ellipse {
                Width = 8, Height = 8,
                Fill   = new SolidColorBrush(Color.FromRgb(243, 139, 168)),
                Stroke = Brushes.White, StrokeThickness = 1,
                ToolTip = $"X-ing: {r2.Id} ({r2.Type})\n" +
                          $"X-Inv: {z2Cross:F2} ft\n" +
                          $"Clearance: {clearance:F2} ft\n" +
                          (clearance < 0 ? "⚠ CONFLICT — negative clearance!" : "")
            };
            Canvas.SetLeft(dot, xPlot - 4);
            Canvas.SetTop(dot, yPlot - 4);
            ProfileCanvas.Children.Add(dot);

            // Clearance annotation
            bool conflict = Math.Abs(clearance) < 0.5;
            var xLbl = new TextBlock {
                Text = conflict ? $"⚠ CLR={clearance:F2}'" : $"CLR={clearance:F2}'",
                Foreground = conflict
                    ? new SolidColorBrush(Color.FromRgb(243, 139, 168))
                    : new SolidColorBrush(Color.FromRgb(250, 179, 135)),
                FontSize = 9, FontFamily = new FontFamily("Consolas"),
                Background = new SolidColorBrush(Color.FromArgb(160, 15, 15, 23))
            };
            Canvas.SetLeft(xLbl, xPlot + 6);
            Canvas.SetTop(xLbl, yPlot - 22);
            ProfileCanvas.Children.Add(xLbl);
        }
    }

    // ── Y-axis labels ──────────────────────────────────────────────────────────
    private void DrawYAxisLabels()
    {
        double zRange   = (_topZ - _baseZ) * VertExag;
        double interval = NiceInterval(zRange / 6.0);
        double startTick = Math.Ceiling(_baseZ / interval) * interval;
        var brush = new SolidColorBrush(Color.FromRgb(166, 173, 200));

        for (double z = startTick; z <= _topZ + 0.01; z += interval)
        {
            double y = GetY(z);
            if (y < 0 || y > _canvasH) continue;

            var tb = new TextBlock {
                Text = $"{z:F1}", FontSize = 9,
                Foreground = brush, FontFamily = new FontFamily("Consolas"),
                TextAlignment = TextAlignment.Right, Width = 50
            };
            Canvas.SetLeft(tb, 2);
            Canvas.SetTop(tb, y - 7);
            YAxisCanvas.Children.Add(tb);
        }

        // "ELEV (ft)" label rotated
        var header = new TextBlock {
            Text = "ELEV (ft)", FontSize = 8.5,
            Foreground = brush, FontFamily = new FontFamily("Segoe UI"),
            RenderTransform = new RotateTransform(-90),
            RenderTransformOrigin = new Point(0, 0)
        };
        Canvas.SetLeft(header, 10);
        Canvas.SetTop(header, _canvasH / 2 + 28);
        YAxisCanvas.Children.Add(header);
    }

    // ── X-axis labels ──────────────────────────────────────────────────────────
    private void DrawXAxisLabels(IList<PipeRun> runs)
    {
        double interval = NiceInterval(_totalLen / 8.0);
        var brush = new SolidColorBrush(Color.FromRgb(166, 173, 200));

        for (double sta = 0; sta <= _totalLen + 0.01; sta += interval)
        {
            double x = sta * _xScale;
            var tb = new TextBlock {
                Text = FormatStation(sta), FontSize = 8.5,
                Foreground = brush, FontFamily = new FontFamily("Consolas"),
                TextAlignment = TextAlignment.Center, Width = 64
            };
            Canvas.SetLeft(tb, x - 32);
            Canvas.SetTop(tb, 4);
            XAxisCanvas.Children.Add(tb);
        }

        // "STATION (ft)" label
        var header = new TextBlock {
            Text = "STATION (ft)", FontSize = 8.5,
            Foreground = brush, FontFamily = new FontFamily("Segoe UI")
        };
        Canvas.SetLeft(header, _canvasW / 2 - 32);
        Canvas.SetTop(header, 18);
        XAxisCanvas.Children.Add(header);
    }

    // ── Draggable invert nodes ─────────────────────────────────────────────────
    private void AddDraggableNode(PipeRun run, bool isStart, double x, double y,
                                  double initZ, double stationOffset, double len)
    {
        var node = new Ellipse {
            Width = 14, Height = 14,
            Fill = new SolidColorBrush(Color.FromRgb(166, 227, 161)),
            Stroke = Brushes.White, StrokeThickness = 1.5,
            Cursor = Cursors.SizeNS,
            ToolTip  = isStart
                ? $"Drag ↕ to edit Invert Start\nCurrent: {initZ:F3} ft"
                : $"Drag ↕ to edit Invert End\nCurrent: {initZ:F3} ft"
        };

        Canvas.SetLeft(node, x - 7);
        Canvas.SetTop(node,  y - 7);
        ProfileCanvas.Children.Add(node);

        bool dragging = false;
        node.MouseLeftButtonDown += (_, e) => { dragging = true; node.CaptureMouse(); e.Handled = true; };
        node.MouseMove += (_, e) =>
        {
            if (!dragging) return;
            double curY = e.GetPosition(ProfileCanvas).Y;
            curY = Math.Clamp(curY, 0, _canvasH);
            double curZ = GetZ(curY);
            if (isStart) run.InvertStart = Math.Round(curZ, 3);
            else         run.InvertEnd   = Math.Round(curZ, 3);
            Canvas.SetTop(node, curY - 7);
        };
        node.MouseLeftButtonUp += (_, _) =>
        {
            dragging = false;
            node.ReleaseMouseCapture();
            Draw();  // Full redraw with updated inverts
        };
    }

    // ── Mouse crosshair ───────────────────────────────────────────────────────
    private void ProfileCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(ProfileCanvas);
        if (pos.X < 0 || pos.X > _canvasW || _xScale == 0 || _yScale == 0) return;

        double station = pos.X / _xScale;
        double elev    = GetZ(pos.Y);

        TxtCrosshair.Text = $"STA {FormatStation(station)}  |  ELEV {elev:F2} ft";
        TxtCrosshair.Visibility = Visibility.Visible;
        Canvas.SetLeft(TxtCrosshair, Math.Min(pos.X + 12, _canvasW - 160));
        Canvas.SetTop(TxtCrosshair,  Math.Max(pos.Y - 24, 4));
    }

    private void ProfileCanvas_MouseLeave(object sender, MouseEventArgs e)
        => TxtCrosshair.Visibility = Visibility.Collapsed;

    // ── Toolbar handlers ───────────────────────────────────────────────────────
    private void BtnExaggerate_Click(object sender, RoutedEventArgs e)
    {
        _exagIdx = (_exagIdx + 1) % ExagSteps.Length;
        TxtExagLabel.Text = $"Exag: {VertExag}×";
        BtnExaggerate.Content = $"{VertExag}× Vert. Exaggeration";
        Draw();
    }

    private void BtnShowAll_Click(object sender, RoutedEventArgs e)
    {
        _showAllRuns = !_showAllRuns;
        BtnShowAll.Content  = _showAllRuns ? "🔍 Focus Selected Run" : "🔍 Show All Runs";
        BtnShowAll.Foreground = _showAllRuns
            ? new SolidColorBrush(Color.FromRgb(249, 226, 175))
            : new SolidColorBrush(Color.FromRgb(205, 214, 244));
        Draw();
    }

    private void BtnResetZoom_Click(object sender, RoutedEventArgs e) => Draw();

    private void BtnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog {
            Title = "Save Profile Image",
            Filter = "PNG Image|*.png",
            FileName = $"Profile_{_targetRun?.Id ?? "AllRuns"}_{DateTime.Now:yyyyMMdd_HHmm}.png"
        };
        if (dlg.ShowDialog() != true) return;

        var rtb = new RenderTargetBitmap(
            (int)ProfileCanvas.ActualWidth, (int)ProfileCanvas.ActualHeight,
            96, 96, PixelFormats.Pbgra32);
        rtb.Render(ProfileCanvas);

        using var fs = File.Create(dlg.FileName);
        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(rtb));
        enc.Save(fs);

        TxtStatus.Text = $"✅ Saved → {dlg.FileName}";
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Debounce: only redraw if canvas has actually laid out
        Dispatcher.InvokeAsync(Draw, System.Windows.Threading.DispatcherPriority.Render);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>WPF Y: top = high pixel. Survey Z: higher elevation = higher on screen.</summary>
    private double GetY(double z)
    {
        double zAdjusted = (z - _baseZ) * VertExag;
        return _canvasH - zAdjusted * _yScale;
    }

    private double GetZ(double pixelY)
    {
        double zAdjusted = (_canvasH - pixelY) / _yScale;
        return _baseZ + zAdjusted / VertExag;
    }

    private double GetPointElevation(string pointId, double fallback)
    {
        var pt = _job.PointRows.FirstOrDefault(r => r.PointId == pointId);
        return pt?.Elevation ?? fallback;
    }

    private static double NiceInterval(double raw)
    {
        if (raw <= 0) return 1;
        double mag  = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        double frac = raw / mag;
        double nice = frac < 1.5 ? 1 : frac < 3 ? 2 : frac < 7 ? 5 : 10;
        return nice * mag;
    }

    private static string FormatStation(double sta)
    {
        int hundreds  = (int)(sta / 100);
        double remain = sta % 100;
        return $"{hundreds:0}+{remain:00.00}";
    }

    private void DrawNoData(string msg)
    {
        var tb = new TextBlock {
            Text = msg, Foreground = new SolidColorBrush(Color.FromRgb(88, 91, 112)),
            FontSize = 14, FontFamily = new FontFamily("Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment   = VerticalAlignment.Center
        };
        Canvas.SetLeft(tb, _canvasW / 2 - 120);
        Canvas.SetTop(tb,  _canvasH / 2 - 10);
        ProfileCanvas.Children.Add(tb);
    }

    private void SetTitles(PipeRun run)
    {
        double startZ = run.InvertStart ?? GetPointElevation(run.FromPointId, 0);
        double endZ   = run.InvertEnd   ?? GetPointElevation(run.ToPointId,   0);
        TxtTitle.Text    = $"Profile: {run.Id}  ({run.FromPointId} → {run.ToPointId})  ·  {run.Diameter}\" {run.Material}";
        TxtSubtitle.Text = $"Invert Start: {startZ:F2} ft   Invert End: {endZ:F2} ft   " +
                           $"Slope: {run.SlopePercent:F3}%   L: {run.ComputedLength:F2} ft";
    }
}
