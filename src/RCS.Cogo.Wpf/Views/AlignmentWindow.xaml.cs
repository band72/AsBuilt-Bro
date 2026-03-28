using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RCS.Alignments.Core;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views
{
    public partial class AlignmentWindow : Window
    {
        // ── Plan-view drag state ───────────────────────────────────────────
        private Point _lastMousePosition;
        private bool  _isDragging;
        private bool  _isZoomWindowMode;
        private bool  _isZoomDragging;
        private Point _zoomStartPoint;

        public AlignmentWindow(object dataContext)
        {
            InitializeComponent();
            DataContext = dataContext;

            if (dataContext is ShellViewModel vm)
            {
                vm.ZoomExtentsRequested += (s, e) => ZoomExtents();
                vm.ZoomInRequested      += (s, e) => ZoomIn();
                vm.ZoomOutRequested     += (s, e) => ZoomOut();
                vm.ZoomWindowRequested  += (s, e) =>
                {
                    _isZoomWindowMode = true;
                    Mouse.OverrideCursor = Cursors.Cross;
                };
            }

            // Default world transform (Y-flip for survey coords)
            var m = new Matrix();
            m.Scale(1, -1);
            m.Translate(-4400, 5400);
            WorldTransform.Matrix = m;
        }

        // ── Save buttons ───────────────────────────────────────────────────
        private void OnSaveHa(object sender, RoutedEventArgs e)
        {
            if (DataContext is ShellViewModel vm)
                vm.SaveHorizontalAlignmentCommand?.Execute(null);
        }

        private void OnSaveVa(object sender, RoutedEventArgs e)
        {
            if (DataContext is ShellViewModel vm)
                vm.SaveVerticalAlignmentCommand?.Execute(null);
        }

        // ── Profile canvas ─────────────────────────────────────────────────
        private void OnProfileCanvasSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawProfile();
            DrawAlignmentOverlay();
        }

        private void OnRefreshProfile(object sender, RoutedEventArgs e)
        {
            DrawProfile();
            DrawAlignmentOverlay();
        }

        // ── Plan-view alignment overlay ─────────────────────────────────────
        /// <summary>
        /// Draws all in-memory alignments onto the plan-view <see cref="ViewportCanvas"/>
        /// as world-coordinate polylines with station ticks and name labels.
        /// The items share the same MatrixTransform applied to the figures layer,
        /// so they pan/zoom automatically.
        /// </summary>
        private void DrawAlignmentOverlay()
        {
            // Remove previously drawn alignment children (tagged with the overlay tag)
            var toRemove = ViewportCanvas.Children
                .OfType<UIElement>()
                .Where(el =>
                {
                    if (el is Polyline pl) return pl.Tag is string s && s == "AlignOverlay";
                    if (el is Line ln)    return ln.Tag is string s2 && s2 == "AlignOverlay";
                    if (el is TextBlock tb) return tb.Tag is string s3 && s3 == "AlignOverlay";
                    return false;
                })
                .ToList();
            foreach (var el in toRemove) ViewportCanvas.Children.Remove(el);

            var vm = DataContext as ShellViewModel;
            if (vm == null) return;
            var ctx = vm.GetContext();
            if (ctx == null) return;

            var alignments = ctx.GetAllAlignments().ToList();
            if (alignments.Count == 0) return;

            // Color cycle for multiple alignments
            var colors = new[]
            {
                Color.FromRgb(86, 204, 242),   // cyan
                Color.FromRgb(255, 180, 50),   // amber
                Color.FromRgb(120, 220, 130),  // green
                Color.FromRgb(220, 100, 220),  // magenta
            };

            int colorIdx = 0;
            foreach (var algn in alignments)
            {
                if (algn.Elements.Count == 0) { colorIdx++; continue; }

                var clr = colors[colorIdx % colors.Length];
                var brush = new SolidColorBrush(clr);
                colorIdx++;

                // ── CL Polyline (sampled every 5 ft or 50 points, whichever is finer) ──
                double totalLen = algn.Elements.Sum(e => e.Length);
                int steps = Math.Max(50, (int)(totalLen / 5));
                double startSta = algn.Elements.First().StartStation;
                double endSta   = algn.Elements.Last().EndStation;

                var pts = new PointCollection();
                for (int i = 0; i <= steps; i++)
                {
                    double sta  = startSta + (i / (double)steps) * (endSta - startSta);
                    var   coord = algn.GetCoordinateAt(sta);
                    if (coord != null)
                        pts.Add(new Point(coord.Easting, coord.Northing));
                }

                if (pts.Count >= 2)
                {
                    var pl = new Polyline
                    {
                        Points          = pts,
                        Stroke          = brush,
                        StrokeThickness = 0.4,        // world-unit thickness — transforms with zoom
                        StrokeDashArray = new DoubleCollection { 6, 2 },
                        Tag             = "AlignOverlay"
                    };
                    ViewportCanvas.Children.Add(pl);
                }

                // ── Station tick marks ─────────────────────────────────────
                double tickInterval = totalLen > 1000 ? 100 : totalLen > 200 ? 50 : 25;
                for (double sta = startSta; sta <= endSta + 0.01; sta += tickInterval)
                {
                    var clCoord = algn.GetCoordinateAt(sta);
                    if (clCoord == null) continue;

                    // Perpendicular offset ±2 world units for the tick
                    double tickLen = Math.Max(2.0, totalLen * 0.012);
                    var leftPt  = algn.GetCoordinateAt(sta, -tickLen);
                    var rightPt = algn.GetCoordinateAt(sta,  tickLen);
                    if (leftPt == null || rightPt == null) continue;

                    var tick = new Line
                    {
                        X1 = leftPt.Easting,  Y1 = leftPt.Northing,
                        X2 = rightPt.Easting, Y2 = rightPt.Northing,
                        Stroke          = brush,
                        StrokeThickness = 0.3,
                        Opacity         = 0.7,
                        Tag             = "AlignOverlay"
                    };
                    ViewportCanvas.Children.Add(tick);

                    // Station label (at the CL point — scales with the world transform)
                    var lbl = new TextBlock
                    {
                        Text       = RCS.Alignments.Core.StationPoint.FormatStation(sta),
                        FontSize   = Math.Max(1.0, totalLen * 0.008),
                        Foreground = new SolidColorBrush(Color.FromArgb(180, clr.R, clr.G, clr.B)),
                        Tag        = "AlignOverlay"
                    };
                    Canvas.SetLeft(lbl, clCoord.Easting + tickLen * 0.3);
                    Canvas.SetTop(lbl,  clCoord.Northing);
                    ViewportCanvas.Children.Add(lbl);
                }

                // ── Alignment name label at start ──────────────────────────
                var startCoord = algn.GetCoordinateAt(startSta);
                if (startCoord != null)
                {
                    var nameLbl = new TextBlock
                    {
                        Text       = algn.Name,
                        FontSize   = Math.Max(1.5, totalLen * 0.012),
                        FontWeight = FontWeights.SemiBold,
                        Foreground = brush,
                        Tag        = "AlignOverlay"
                    };
                    Canvas.SetLeft(nameLbl, startCoord.Easting);
                    Canvas.SetTop(nameLbl,  startCoord.Northing);
                    ViewportCanvas.Children.Add(nameLbl);
                }
            }
        }

        private void DrawProfile()
        {
            ProfileCanvas.Children.Clear();
            var vm = DataContext as ShellViewModel;
            if (vm == null) return;

            var ctx = vm.GetContext();
            if (ctx == null) return;

            // Find the first alignment that has at least one profile
            var allAlignments = ctx.GetAllAlignments().ToList();
            Alignment? algn = allAlignments.FirstOrDefault(a => a.Profiles.Count > 0)
                           ?? allAlignments.FirstOrDefault();
            if (algn == null) return;

            double canvasW = ProfileCanvas.ActualWidth;
            double canvasH = ProfileCanvas.ActualHeight;
            if (canvasW < 10 || canvasH < 10) return;

            const double lm = 55, rm = 20, tm = 28, bm = 40;
            double plotW = canvasW - lm - rm;
            double plotH = canvasH - tm - bm;

            double totalLen = algn.Elements.Sum(e => e.Length);
            if (totalLen < 0.01) totalLen = 100;

            // ── Collect elevation range from all profiles ──────────────────
            var egProf = algn.Profiles.FirstOrDefault(p =>
                p.ProfileType.Equals("EG", StringComparison.OrdinalIgnoreCase));
            var fgProf = algn.Profiles.FirstOrDefault(p =>
                p.ProfileType.Equals("FG", StringComparison.OrdinalIgnoreCase));

            var allElevs = new List<double>();
            void CollectElevs(Profile? p)
            {
                if (p == null) return;
                for (double sta = 0; sta <= totalLen; sta += totalLen / 20)
                {
                    double? e = p.GetElevationAtStation(sta);
                    if (e.HasValue) allElevs.Add(e.Value);
                }
            }
            CollectElevs(egProf);
            CollectElevs(fgProf);

            // Fallback if no profiles
            if (allElevs.Count == 0) { allElevs.Add(90); allElevs.Add(100); }

            double elvMin = allElevs.Min() - 1.5;
            double elvMax = allElevs.Max() + 2.0;
            double elvRange = elvMax - elvMin;
            if (elvRange < 1) elvRange = 1;

            double ToX(double sta)   => lm + sta / totalLen * plotW;
            double ToY(double elev)  => tm + (1.0 - (elev - elvMin) / elvRange) * plotH;

            // ── Grid ──────────────────────────────────────────────────────
            DrawProfileGrid(totalLen, elvMin, elvMax, elvRange, ToX, ToY, plotH, tm, bm, canvasW);

            // ── EG Polyline ──────────────────────────────────────────────
            if (egProf != null)
                DrawProfileLine(egProf, totalLen, ToX, ToY,
                    new SolidColorBrush(Color.FromRgb(80, 160, 240)), 1.5, isDashed: true);

            // ── FG Polyline + grade labels ───────────────────────────────
            if (fgProf != null)
            {
                DrawProfileLine(fgProf, totalLen, ToX, ToY,
                    new SolidColorBrush(Color.FromRgb(255, 210, 50)), 2.0, isDashed: false);
                DrawGradeLabels(fgProf, totalLen, ToX, ToY);
                DrawVpiDots(fgProf, total: totalLen, toX: ToX, toY: ToY);
            }

            // ── Station axis ──────────────────────────────────────────────
            double staStep = totalLen > 500 ? 100 : totalLen > 200 ? 50 : 25;
            for (double sta = 0; sta <= totalLen + 0.5; sta += staStep)
            {
                double x = ToX(sta);
                double y = tm + plotH;
                DrawLine(x, y, x, y + 5, Brushes.Gray);
                var lbl = new TextBlock
                {
                    Text     = StationPoint.FormatStation(sta),
                    FontSize = 9,
                    Foreground = Brushes.Gray
                };
                Canvas.SetLeft(lbl, x - 16);
                Canvas.SetTop(lbl, y + 7);
                ProfileCanvas.Children.Add(lbl);
            }

            // ── Elevation axis ────────────────────────────────────────────
            double elvStep2 = elvRange > 10 ? 2 : 1;
            for (double elv = Math.Ceiling(elvMin / elvStep2) * elvStep2; elv <= elvMax; elv += elvStep2)
            {
                double y = ToY(elv);
                DrawLine(lm - 5, y, lm, y, Brushes.Gray);
                var lbl = new TextBlock { Text = $"{elv:F0}", FontSize = 9, Foreground = Brushes.Gray };
                Canvas.SetLeft(lbl, 2);
                Canvas.SetTop(lbl, y - 8);
                ProfileCanvas.Children.Add(lbl);
            }

            // ── Alignment name label ──────────────────────────────────────
            var title = new TextBlock
            {
                Text = $"{algn.Name}  (Sta 0+00 – {StationPoint.FormatStation(totalLen)})",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 200, 255))
            };
            Canvas.SetLeft(title, lm + 5);
            Canvas.SetTop(title, 6);
            ProfileCanvas.Children.Add(title);
        }

        private void DrawProfileGrid(double totalLen, double elvMin, double elvMax, double elvRange,
            Func<double, double> toX, Func<double, double> toY,
            double plotH, double tm, double bm, double canvasW)
        {
            // Horizontal grid (elevations)
            double elvStep = elvRange > 10 ? 2 : 1;
            for (double elv = Math.Ceiling(elvMin / elvStep) * elvStep; elv <= elvMax; elv += elvStep)
            {
                double y = toY(elv);
                var line = new Line { X1 = 55, Y1 = y, X2 = canvasW - 20, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                    StrokeThickness = 0.5 };
                ProfileCanvas.Children.Add(line);
            }
            // Vertical grid (stations)
            double staStep = totalLen > 500 ? 100 : totalLen > 200 ? 50 : 25;
            for (double sta = 0; sta <= totalLen; sta += staStep)
            {
                double x = toX(sta);
                var line = new Line { X1 = x, Y1 = tm, X2 = x, Y2 = tm + plotH,
                    Stroke = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    StrokeThickness = 0.5 };
                ProfileCanvas.Children.Add(line);
            }
        }

        private void DrawProfileLine(Profile prof, double totalLen,
            Func<double, double> toX, Func<double, double> toY,
            Brush stroke, double thickness, bool isDashed)
        {
            var pc = new PointCollection();
            int steps = Math.Max(100, (int)(totalLen / 5));
            for (int i = 0; i <= steps; i++)
            {
                double sta  = i / (double)steps * totalLen;
                double? elv = prof.GetElevationAtStation(sta);
                if (elv.HasValue)
                    pc.Add(new Point(toX(sta), toY(elv.Value)));
            }
            if (pc.Count < 2) return;

            var pl = new Polyline
            {
                Points          = pc,
                Stroke          = stroke,
                StrokeThickness = thickness,
                StrokeLineJoin  = PenLineJoin.Round
            };
            if (isDashed)
                pl.StrokeDashArray = new DoubleCollection { 8, 4 };
            ProfileCanvas.Children.Add(pl);
        }

        private void DrawGradeLabels(Profile prof, double totalLen,
            Func<double, double> toX, Func<double, double> toY)
        {
            var vpis = prof.Intersections.OrderBy(v => v.Station).ToList();
            for (int i = 0; i < vpis.Count - 1; i++)
            {
                double sMid = (vpis[i].Station + vpis[i + 1].Station) / 2.0;
                double eDiff = vpis[i + 1].Elevation - vpis[i].Elevation;
                double sDiff = vpis[i + 1].Station   - vpis[i].Station;
                if (Math.Abs(sDiff) < 0.01) continue;
                double grade = eDiff / sDiff * 100.0;

                double? elvMid = prof.GetElevationAtStation(sMid);
                if (!elvMid.HasValue) continue;

                string label = $"{grade:+0.00;-0.00}%";
                var t = new TextBlock
                {
                    Text       = label,
                    FontSize   = 9,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = grade >= 0
                        ? new SolidColorBrush(Color.FromRgb(255, 210, 50))
                        : new SolidColorBrush(Color.FromRgb(255, 140, 80))
                };
                Canvas.SetLeft(t, toX(sMid) - 18);
                Canvas.SetTop(t,  toY(elvMid.Value) - 18);
                ProfileCanvas.Children.Add(t);
            }
        }

        private void DrawVpiDots(Profile prof, double total,
            Func<double, double> toX, Func<double, double> toY)
        {
            foreach (var vpi in prof.Intersections)
            {
                if (vpi.Station < -0.01 || vpi.Station > total + 0.01) continue;
                double x = toX(vpi.Station);
                double y = toY(vpi.Elevation);
                var dot = new Ellipse { Width = 6, Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 180, 50)) };
                Canvas.SetLeft(dot, x - 3);
                Canvas.SetTop(dot,  y - 3);
                ProfileCanvas.Children.Add(dot);

                // Elev label
                var lbl = new TextBlock
                {
                    Text       = $"{vpi.Elevation:F2}",
                    FontSize   = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 80))
                };
                Canvas.SetLeft(lbl, x + 5);
                Canvas.SetTop(lbl,  y - 14);
                ProfileCanvas.Children.Add(lbl);
            }
        }

        private void DrawLine(double x1, double y1, double x2, double y2, Brush stroke)
        {
            ProfileCanvas.Children.Add(new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = stroke, StrokeThickness = 1
            });
        }

        // ── Plan-view zoom/pan ─────────────────────────────────────────────
        private void ZoomIn()
        {
            var center = new Point(ViewportCanvas.ActualWidth / 2, ViewportCanvas.ActualHeight / 2);
            var m = WorldTransform.Matrix;
            m.ScaleAt(1.05, 1.05, center.X, center.Y);
            WorldTransform.Matrix = m;
            if (DataContext is ShellViewModel vm) vm.CurrentViewScale = m.M11;
        }

        private void ZoomOut()
        {
            var center = new Point(ViewportCanvas.ActualWidth / 2, ViewportCanvas.ActualHeight / 2);
            var m = WorldTransform.Matrix;
            m.ScaleAt(1.0 / 1.05, 1.0 / 1.05, center.X, center.Y);
            WorldTransform.Matrix = m;
            if (DataContext is ShellViewModel vm) vm.CurrentViewScale = m.M11;
        }

        private void ZoomExtents()
        {
            var vm = DataContext as ShellViewModel;
            if (vm == null || vm.Points.Count == 0) return;

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            foreach (var p in vm.Points)
            {
                if (p.Easting  < minX) minX = p.Easting;
                if (p.Easting  > maxX) maxX = p.Easting;
                if (p.Northing < minY) minY = p.Northing;
                if (p.Northing > maxY) maxY = p.Northing;
            }
            foreach (var f in vm.Figures)
                foreach (var pt in f.Points)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.Y > maxY) maxY = pt.Y;
                }

            if (minX == double.MaxValue) return;
            double wid = Math.Max(maxX - minX, 10) * 1.2;
            double hgt = Math.Max(maxY - minY, 10) * 1.2;
            double scale = Math.Min(ViewportCanvas.ActualWidth / wid, ViewportCanvas.ActualHeight / hgt);
            double midX = (minX + maxX) / 2, midY = (minY + maxY) / 2;

            var mtx = new Matrix();
            mtx.Scale(scale, -scale);
            mtx.Translate(ViewportCanvas.ActualWidth  / 2 - midX * scale,
                          ViewportCanvas.ActualHeight / 2 + midY * scale);
            WorldTransform.Matrix = mtx;
            if (vm != null) vm.CurrentViewScale = scale;
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var pos = e.GetPosition(ViewportCanvas);
            var m   = WorldTransform.Matrix;
            double s = e.Delta > 0 ? 1.03 : (1.0 / 1.03);
            m.ScaleAt(s, s, pos.X, pos.Y);
            WorldTransform.Matrix = m;
            if (DataContext is ShellViewModel vm) vm.CurrentViewScale = m.M11;
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMousePosition = e.GetPosition(this);
            _isDragging = true;
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ((UIElement)sender).ReleaseMouseCapture();
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var cur = e.GetPosition(this);
                var delta = cur - _lastMousePosition;
                var m = WorldTransform.Matrix;
                m.Translate(delta.X, delta.Y);
                WorldTransform.Matrix = m;
                _lastMousePosition = cur;
                e.Handled = true;
            }
            else if (_isZoomDragging)
            {
                var pos = e.GetPosition((UIElement)sender);
                ZoomRect.Margin = new Thickness(
                    Math.Min(pos.X, _zoomStartPoint.X),
                    Math.Min(pos.Y, _zoomStartPoint.Y), 0, 0);
                ZoomRect.Width  = Math.Abs(pos.X - _zoomStartPoint.X);
                ZoomRect.Height = Math.Abs(pos.Y - _zoomStartPoint.Y);
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isZoomWindowMode) return;
            _isZoomDragging = true;
            _zoomStartPoint = e.GetPosition((UIElement)sender);
            ZoomRect.Margin = new Thickness(_zoomStartPoint.X, _zoomStartPoint.Y, 0, 0);
            ZoomRect.Width = ZoomRect.Height = 0;
            ZoomRect.Visibility = Visibility.Visible;
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isZoomDragging) return;
            _isZoomDragging = _isZoomWindowMode = false;
            Mouse.OverrideCursor = null;
            ZoomRect.Visibility  = Visibility.Collapsed;
            ((UIElement)sender).ReleaseMouseCapture();

            var endPt = e.GetPosition((UIElement)sender);
            if (Math.Abs(endPt.X - _zoomStartPoint.X) > 5 &&
                Math.Abs(endPt.Y - _zoomStartPoint.Y) > 5)
            {
                var inv = WorldTransform.Matrix;
                inv.Invert();
                var ws = inv.Transform(_zoomStartPoint);
                var we = inv.Transform(endPt);
                double minX = Math.Min(ws.X, we.X), maxX = Math.Max(ws.X, we.X);
                double minY = Math.Min(ws.Y, we.Y), maxY = Math.Max(ws.Y, we.Y);
                double wid  = Math.Max(maxX - minX, 1);
                double hgt  = Math.Max(maxY - minY, 1);
                double vpW  = ViewportCanvas.ActualWidth;
                double vpH  = ViewportCanvas.ActualHeight;
                if (vpW > 0 && vpH > 0)
                {
                    double scale = Math.Min(vpW / wid, vpH / hgt);
                    double midX  = (minX + maxX) / 2, midY = (minY + maxY) / 2;
                    var m = new Matrix();
                    m.Scale(scale, -scale);
                    m.Translate(vpW / 2 - midX * scale, vpH / 2 + midY * scale);
                    WorldTransform.Matrix = m;
                    if (DataContext is ShellViewModel vm) vm.CurrentViewScale = scale;
                }
            }
            e.Handled = true;
        }
    }
}
