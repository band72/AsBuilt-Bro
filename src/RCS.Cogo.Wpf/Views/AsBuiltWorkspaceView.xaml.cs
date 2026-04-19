using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

/// <summary>
/// Code-behind for the four-pane As-Built production workspace.
/// All business logic lives in <see cref="AsBuiltWorkspaceViewModel"/>;
/// this file is responsible for the live CAD viewer and two-way selection
/// sync between the viewer and the center-pane data grids.
/// </summary>
public partial class AsBuiltWorkspaceView : UserControl
{
    private AsBuiltWorkspaceViewModel? _vm;

    // Reference to ShellWindow's shared canvas (for pan/zoom operations)
    private Canvas? _injectedCanvas;

    // Dedicated WPF Canvas rendered inside CanvasRegion for the live network view
    private readonly Canvas    _liveCanvas = new() { Background = Brushes.Transparent };
    private readonly TextBlock _watermark  = new()
    {
        Text                = "Live CAD Viewer",
        Foreground          = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x45)),
        FontSize            = 20,
        FontWeight          = FontWeights.Bold,
        FontFamily          = new FontFamily("Segoe UI"),
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment   = VerticalAlignment.Center
    };

    public AsBuiltWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // After layout: place _liveCanvas and watermark into the CanvasRegion border.
        Loaded += (_, _) =>
        {
            if (CanvasRegion.Child == null)
            {
                var grid = new Grid();
                grid.Children.Add(_liveCanvas);
                grid.Children.Add(_watermark);
                CanvasRegion.Child = grid;
            }
            RefreshLiveViewer();
        };

        // Redraw when canvas resizes
        _liveCanvas.SizeChanged += (_, _) => RefreshLiveViewer();

        // Register for drag drop overlay triggers
        AllowDrop = true;
        DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) DragDropOverlay.Visibility = Visibility.Visible; };
        DragLeave += (s, e) => { DragDropOverlay.Visibility = Visibility.Collapsed; };
    }

    // ── Snackbar / Toasts ───────────────────────────────────────────────────
    public void ShowSnackbar(string message, bool isError = false)
    {
        SnackbarText.Text = message;
        SnackbarText.Foreground = isError
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F38BA8"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A6E3A1"));

        var slideIn = new System.Windows.Media.Animation.ThicknessAnimation
        {
            From = new Thickness(0, 0, 0, -100),
            To   = new Thickness(0, 0, 0, 24),
            Duration = TimeSpan.FromMilliseconds(300),
            DecelerationRatio = 0.9
        };
        var fadeIn = new System.Windows.Media.Animation.DoubleAnimation
        {
            From = 0.0, To = 1.0, Duration = TimeSpan.FromMilliseconds(300)
        };

        var sb = new System.Windows.Media.Animation.Storyboard();
        sb.Children.Add(slideIn);
        sb.Children.Add(fadeIn);
        System.Windows.Media.Animation.Storyboard.SetTarget(slideIn, SnackbarOverlay);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(slideIn, new PropertyPath("Margin"));
        System.Windows.Media.Animation.Storyboard.SetTarget(fadeIn, SnackbarOverlay);
        System.Windows.Media.Animation.Storyboard.SetTargetProperty(fadeIn, new PropertyPath("Opacity"));

        sb.Completed += async (_, _) =>
        {
            await System.Threading.Tasks.Task.Delay(3500);
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                { To = 0.0, Duration = TimeSpan.FromMilliseconds(300) };
            SnackbarOverlay.BeginAnimation(OpacityProperty, fadeOut);
        };

        sb.Begin();
    }

    // ── Drag & Drop Intake ──────────────────────────────────────────────────
    private async void OnWorkspaceDrop(object sender, DragEventArgs e)
    {
        DragDropOverlay.Visibility = Visibility.Collapsed;
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0 && _vm != null)
            {
                ShowSnackbar($"Extracting {System.IO.Path.GetFileName(files[0])}...", false);
                await _vm.LoadDragDropFileAsync(files[0]);
                ShowSnackbar("Extraction Complete! Dashboard Live.", false);
            }
        }
    }

    // ── Canvas Injection ──────────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="ShellWindow"/> on first As-Built tab activation.
    /// Stores the shared canvas reference and triggers the initial live render.
    /// </summary>
    public void InjectCanvas(Canvas canvas)
    {
        _injectedCanvas = canvas;
        RefreshLiveViewer();
    }

    // ── Live Network Renderer ─────────────────────────────────────────────────

    /// <summary>
    /// Draws all pipe runs and structures into <see cref="_liveCanvas"/>.
    ///
    /// DATA PRIORITY:
    ///   1. <see cref="AsBuiltWorkspaceViewModel.Job"/> when it has data
    ///      (PointRows > 0 or Network has geometry) — covers AI-extracted
    ///      blueprints, COGO imports, and JEA Excel imports.
    ///   2. ShellViewModel (shared COGO point list) — fallback for the legacy
    ///      point-list view used outside the As-Built workflow.
    /// </summary>
    private void RefreshLiveViewer()
    {
        _liveCanvas.Children.Clear();

        var job = _vm?.Job;
        bool hasAsBuiltData = job != null &&
            (job.PointRows.Count > 0 ||
             job.Network.Structures.Count > 0 ||
             job.Network.Runs.Count > 0);

        if (hasAsBuiltData)
            DrawFromAsBuiltJob(job!);
        else
            DrawFromShellViewModel();
    }

    // ── AsBuiltJob renderer ───────────────────────────────────────────────────

    private void DrawFromAsBuiltJob(RCS.Piping.Core.Workflow.AsBuiltJob job)
    {
        if (job.PointRows.Count == 0 && job.Network.Runs.Count == 0 && job.Network.Structures.Count == 0)
        {
            ShowWatermark("No pipeline data — import a file or drop a blueprint.");
            return;
        }

        _watermark.Visibility = Visibility.Collapsed;

        var ptDict       = job.PointRows.ToDictionary(r => r.PointId, r => r);
        var allNorthings = job.PointRows.Select(p => p.Northing).ToList();
        var allEastings  = job.PointRows.Select(p => p.Easting).ToList();

        if (allNorthings.Count == 0) { ShowWatermark("Points have no coordinates yet."); return; }

        double minN = allNorthings.Min(), maxN = allNorthings.Max();
        double minE = allEastings.Min(),  maxE = allEastings.Max();
        double worldW = maxE - minE; if (worldW < 1e-6) worldW = 100;
        double worldH = maxN - minN; if (worldH < 1e-6) worldH = 100;

        double sw = _liveCanvas.ActualWidth  > 10 ? _liveCanvas.ActualWidth  : 500;
        double sh = _liveCanvas.ActualHeight > 10 ? _liveCanvas.ActualHeight : 400;

        const double padPx = 30;
        double scale = Math.Min((sw - padPx * 2) / worldW, (sh - padPx * 2) / worldH);
        double ox    = padPx + (sw - padPx * 2 - worldW * scale) / 2.0;
        double oy    = padPx + (sh - padPx * 2 - worldH * scale) / 2.0;

        double Sx(double e) => ox + (e - minE) * scale;
        double Sy(double n) => sh - (oy + (n - minN) * scale);  // flip Y

        // ── Pipe runs ────────────────────────────────────────────────────────
        foreach (var run in job.Network.Runs.Values)
        {
            if (!ptDict.TryGetValue(run.FromPointId, out var fp)) continue;
            if (!ptDict.TryGetValue(run.ToPointId,   out var tp)) continue;

            double x1 = Sx(fp.Easting), y1 = Sy(fp.Northing);
            double x2 = Sx(tp.Easting), y2 = Sy(tp.Northing);
            bool   sel   = (run.Id == _vm?.SelectedRun?.Id);
            var    brush = ResolveRunBrush(run.Type ?? "");

            if (sel)
                _liveCanvas.Children.Add(new Line
                {
                    X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                    Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    StrokeThickness = 5
                });

            _liveCanvas.Children.Add(new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke          = sel ? Brushes.White : brush,
                StrokeThickness = sel ? 2.5 : 1.8,
                ToolTip = $"{run.Type}  \u00d8{run.Diameter}\" {run.Material}\n" +
                          $"{run.FromPointId} \u2192 {run.ToPointId}\n" +
                          $"L={run.ComputedLength:F1}ft  S={run.SlopePercent:F3}%\n" +
                          $"Inv: {run.InvertStart:F2}' \u2192 {run.InvertEnd:F2}'"
            });

            DrawFlowArrow(x1, y1, x2, y2, brush);

            double lenPx = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            if (lenPx > 50)
            {
                var lbl = new TextBlock
                {
                    Text       = $"{run.Diameter}\" {run.Material}",
                    FontSize   = 8,  Foreground = brush,
                    FontFamily = new FontFamily("Consolas"),
                    Background = new SolidColorBrush(Color.FromArgb(140, 13, 13, 23))
                };
                Canvas.SetLeft(lbl, (x1 + x2) / 2 + 4);
                Canvas.SetTop(lbl,  (y1 + y2) / 2 - 10);
                _liveCanvas.Children.Add(lbl);
            }
        }

        // ── Structure glyphs ─────────────────────────────────────────────────
        foreach (var st in job.Network.Structures.Values)
        {
            if (!ptDict.TryGetValue(st.PointId, out var pt)) continue;

            double cx  = Sx(pt.Easting), cy = Sy(pt.Northing);
            bool   sel = (st.Id == _vm?.SelectedStructure?.Id);
            double r   = sel ? 8 : 5.5;
            var    sb2 = ResolveStructBrush(st.Type ?? "");

            if (sel)
            {
                var glow = new Ellipse
                {
                    Width = (r + 4) * 2, Height = (r + 4) * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb(90, 255, 215, 0)),
                    StrokeThickness = 3, Fill = Brushes.Transparent
                };
                Canvas.SetLeft(glow, cx - r - 4); Canvas.SetTop(glow, cy - r - 4);
                _liveCanvas.Children.Add(glow);
            }

            var ring = new Ellipse
            {
                Width = r * 2, Height = r * 2,
                Stroke = sel ? Brushes.Gold : sb2,
                Fill   = new SolidColorBrush(Color.FromArgb(
                    sel ? (byte)180 : (byte)120, sb2.Color.R, sb2.Color.G, sb2.Color.B)),
                StrokeThickness = sel ? 2.5 : 1.2,
                ToolTip = $"{st.Type}  [{st.PointId}]\n" +
                          (st.RimElevation.HasValue ? $"Rim: {st.RimElevation:F2}'\n" : "") +
                          (st.InvertOut.HasValue    ? $"Inv Out: {st.InvertOut:F2}'"  : "")
            };
            Canvas.SetLeft(ring, cx - r); Canvas.SetTop(ring, cy - r);
            _liveCanvas.Children.Add(ring);

            var lbl = new TextBlock
            {
                Text       = st.PointId, FontSize = 9,
                Foreground = sel ? Brushes.Gold : new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = sel ? FontWeights.Bold : FontWeights.Normal,
                Background = new SolidColorBrush(Color.FromArgb(140, 13, 13, 23))
            };
            Canvas.SetLeft(lbl, cx + r + 3); Canvas.SetTop(lbl, cy - 8);
            _liveCanvas.Children.Add(lbl);
        }

        // ── Elevation labels (only for small networks) ────────────────────────
        if (job.PointRows.Count <= 50)
        {
            foreach (var pt in job.PointRows)
            {
                if (pt.Elevation == 0) continue;
                var elLbl = new TextBlock
                {
                    Text = $"{pt.Elevation:F1}'", FontSize = 8,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x74, 0xC7, 0xEC)),
                    FontFamily = new FontFamily("Consolas"),
                    Background = new SolidColorBrush(Color.FromArgb(100, 13, 13, 23))
                };
                Canvas.SetLeft(elLbl, Sx(pt.Easting) + 5);
                Canvas.SetTop(elLbl,  Sy(pt.Northing) + 4);
                _liveCanvas.Children.Add(elLbl);
            }
        }

        DrawLegend(job);
    }

    // ── Flow arrow ────────────────────────────────────────────────────────────

    private void DrawFlowArrow(double x1, double y1, double x2, double y2, SolidColorBrush brush)
    {
        double mx = (x1 + x2) / 2, my = (y1 + y2) / 2;
        double dx = x2 - x1, dy = y2 - y1;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-6) return;
        double ux = dx / len, uy = dy / len;
        double perX = -uy, perY = ux;
        const double al = 5.0;

        _liveCanvas.Children.Add(new Polygon
        {
            Fill = brush,
            Points = new PointCollection
            {
                new(mx + ux * al,                        my + uy * al),
                new(mx - ux * al * .5 + perX * al * .6, my - uy * al * .5 + perY * al * .6),
                new(mx - ux * al * .5 - perX * al * .6, my - uy * al * .5 - perY * al * .6)
            }
        });
    }

    // ── Legend ────────────────────────────────────────────────────────────────

    private void DrawLegend(RCS.Piping.Core.Workflow.AsBuiltJob job)
    {
        var types = job.Network.Runs.Values.Select(r => r.Type ?? "Unknown").Distinct().Take(6).ToList();
        if (types.Count == 0) return;

        double ly = 8;
        foreach (var type in types)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = ResolveRunBrush(type), Margin = new Thickness(0, 0, 4, 0) });
            row.Children.Add(new TextBlock
            {
                Text = type, FontSize = 8.5,
                Foreground = new SolidColorBrush(Color.FromRgb(0xBA, 0xC2, 0xDE)),
                FontFamily = new FontFamily("Segoe UI"), VerticalAlignment = VerticalAlignment.Center
            });
            row.Background = new SolidColorBrush(Color.FromArgb(140, 13, 13, 23));
            Canvas.SetLeft(row, 8); Canvas.SetTop(row, ly);
            _liveCanvas.Children.Add(row);
            ly += 14;
        }
    }

    // ── ShellViewModel fallback ───────────────────────────────────────────────

    private void DrawFromShellViewModel()
    {
        var shellVm = GetShellViewModel();
        if (shellVm == null || shellVm.Points.Count == 0) { ShowWatermark("No data loaded."); return; }
        _watermark.Visibility = Visibility.Collapsed;

        double minN = shellVm.Points.Min(p => p.Northing), maxN = shellVm.Points.Max(p => p.Northing);
        double minE = shellVm.Points.Min(p => p.Easting),  maxE = shellVm.Points.Max(p => p.Easting);
        double worldW = maxE - minE; if (worldW < 1e-6) worldW = 1;
        double worldH = maxN - minN; if (worldH < 1e-6) worldH = 1;
        double sw = _liveCanvas.ActualWidth  > 10 ? _liveCanvas.ActualWidth  : 500;
        double sh = _liveCanvas.ActualHeight > 10 ? _liveCanvas.ActualHeight : 400;
        const double padPx = 24;
        double scale = Math.Min((sw - padPx * 2) / worldW, (sh - padPx * 2) / worldH);
        double ox    = padPx + (sw - padPx * 2 - worldW * scale) / 2.0;
        double oy    = padPx + (sh - padPx * 2 - worldH * scale) / 2.0;
        double Sx(double e) => ox + (e - minE) * scale;
        double Sy(double n) => sh - (oy + (n - minN) * scale);

        var ptDict = shellVm.Points.ToDictionary(p => p.Id, p => p);
        foreach (var run in shellVm.PipeRuns)
        {
            if (!ptDict.TryGetValue(run.FromPointId, out var fp)) continue;
            if (!ptDict.TryGetValue(run.ToPointId,   out var tp)) continue;
            _liveCanvas.Children.Add(new Line
            {
                X1 = Sx(fp.Easting), Y1 = Sy(fp.Northing),
                X2 = Sx(tp.Easting), Y2 = Sy(tp.Northing),
                Stroke = ResolveRunBrush(run.Type), StrokeThickness = 1.8,
                ToolTip = $"{run.Type}  \u00d8{run.Diameter}\" {run.Material}\n{run.FromPointId}\u2192{run.ToPointId}"
            });
        }
        foreach (var sg in shellVm.StructureGraphics)
        {
            double cx = Sx(sg.Easting), cy = Sy(sg.Northing);
            double r  = sg.IsSelected ? 7 : 5;
            var ring = new Ellipse
            {
                Width = r * 2, Height = r * 2,
                Stroke = sg.IsSelected ? Brushes.Gold : Brushes.DimGray,
                Fill   = sg.IsSelected
                    ? new SolidColorBrush(Color.FromArgb(120, 255, 215, 0))
                    : new SolidColorBrush(Color.FromArgb(180, 40, 60, 80)),
                StrokeThickness = sg.IsSelected ? 2.5 : 1.2, ToolTip = $"{sg.Type}  [{sg.Id}]"
            };
            Canvas.SetLeft(ring, cx - r); Canvas.SetTop(ring, cy - r);
            _liveCanvas.Children.Add(ring);
        }
        if (shellVm.Points.Count <= 200)
        {
            foreach (var pt in shellVm.Points)
            {
                var lbl = new TextBlock
                {
                    Text = pt.Id, FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7A, 0x9E, 0xCE)),
                    FontFamily = new FontFamily("Segoe UI")
                };
                Canvas.SetLeft(lbl, Sx(pt.Easting) + 5); Canvas.SetTop(lbl, Sy(pt.Northing) - 10);
                _liveCanvas.Children.Add(lbl);
            }
        }
    }

    // ── Watermark helper ──────────────────────────────────────────────────────
    private void ShowWatermark(string msg)
    {
        _watermark.Text       = msg;
        _watermark.Visibility = Visibility.Visible;
    }

    // ── Color resolution ──────────────────────────────────────────────────────

    private static SolidColorBrush ResolveRunBrush(string type)
    {
        string t = (type ?? "").ToUpperInvariant();
        if (t.Contains("FORCE") || t.Contains("PRESSURE"))  return new SolidColorBrush(Color.FromRgb( 80, 200, 120));
        if (t.Contains("WASTE") || t.Contains("SEWER"))     return new SolidColorBrush(Color.FromRgb(100, 180,  60));
        if (t.Contains("WATER"))                             return new SolidColorBrush(Color.FromRgb(  0, 180, 220));
        if (t.Contains("STORM") || t.Contains("DRAIN"))     return new SolidColorBrush(Color.FromRgb(255, 210,  30));
        if (t.Contains("GAS"))                               return new SolidColorBrush(Color.FromRgb(220, 120,  50));
        if (t.Contains("ELEC") || t.Contains("CONDUIT"))    return new SolidColorBrush(Color.FromRgb(200,  70, 200));
        if (t.Contains("TELECOM") || t.Contains("FIBER"))   return new SolidColorBrush(Color.FromRgb(150, 130, 210));
        if (t.Contains("RECLAIM") || t.Contains("REUSE"))   return new SolidColorBrush(Color.FromRgb(180,  80, 200));
        return new SolidColorBrush(Color.FromRgb(140, 140, 160));
    }

    private static SolidColorBrush ResolveStructBrush(string type)
    {
        string t = (type ?? "").ToUpperInvariant();
        if (t.Contains("MANHOLE") || t.Contains("MH") || t.Contains("SAN")) return new SolidColorBrush(Color.FromRgb(100, 180,  60));
        if (t.Contains("VALVE")   || t.Contains("GATE"))                     return new SolidColorBrush(Color.FromRgb(  0, 180, 220));
        if (t.Contains("HYDRANT"))                                            return new SolidColorBrush(Color.FromRgb(220,  60,  60));
        if (t.Contains("INLET")   || t.Contains("JUNCTION"))                 return new SolidColorBrush(Color.FromRgb(255, 210,  30));
        if (t.Contains("VAULT")   || t.Contains("PULL"))                     return new SolidColorBrush(Color.FromRgb(200,  70, 200));
        if (t.Contains("METER"))                                              return new SolidColorBrush(Color.FromRgb(  0, 200, 200));
        return new SolidColorBrush(Color.FromRgb(137, 180, 250));
    }

    // ── DataContext Wiring ────────────────────────────────────────────────────

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.StructureSelectionChanged -= OnStructureSelectionChanged;
            _vm.RunSelectionChanged       -= OnRunSelectionChanged;
            _vm.ShowSnackbarRequested     -= ShowSnackbar;
            _vm.PropertyChanged           -= OnVmPropertyChanged;
            if (_vm.Job?.PointRows is System.Collections.Specialized.INotifyCollectionChanged oldPts)
                oldPts.CollectionChanged -= OnJobCollectionChanged;
        }

        _vm = e.NewValue as AsBuiltWorkspaceViewModel;
        if (_vm == null) return;

        _vm.StructureSelectionChanged += OnStructureSelectionChanged;
        _vm.RunSelectionChanged       += OnRunSelectionChanged;
        _vm.ShowSnackbarRequested     += ShowSnackbar;

        // Key fix: refresh whenever the job object or its import data changes
        _vm.PropertyChanged += OnVmPropertyChanged;
        AttachJobCollectionListeners(_vm.Job);
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not ("Job" or "IntakeReport" or "SelectedRun" or "SelectedStructure"))
            return;

        if (e.PropertyName == "Job" && _vm != null)
            AttachJobCollectionListeners(_vm.Job);

        if (Dispatcher.CheckAccess()) RefreshLiveViewer();
        else Dispatcher.InvokeAsync(RefreshLiveViewer);
    }

    private void AttachJobCollectionListeners(RCS.Piping.Core.Workflow.AsBuiltJob? job)
    {
        if (job?.PointRows is System.Collections.Specialized.INotifyCollectionChanged pts)
            pts.CollectionChanged += OnJobCollectionChanged;
    }

    private void OnJobCollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (Dispatcher.CheckAccess()) RefreshLiveViewer();
        else Dispatcher.InvokeAsync(RefreshLiveViewer);
    }

    private void OnStructureSelectionChanged(object? sender, RCS.Piping.Core.Models.PipeStructure? structure)
    {
        if (structure == null) return;
        var shellVm = GetShellViewModel();
        if (shellVm != null)
            foreach (var sg in shellVm.StructureGraphics)
                sg.IsSelected = (sg.Id == structure.PointId);
        RefreshLiveViewer();
    }

    private void OnRunSelectionChanged(object? sender, RCS.Piping.Core.Models.PipeRun? run)
    {
        if (run == null) return;
        var shellVm = GetShellViewModel();
        if (shellVm != null)
        {
            string fromId = run.FromPointId ?? "", toId = run.ToPointId ?? "";
            foreach (var fig in shellVm.Figures)
                fig.IsHighlighted = (!string.IsNullOrEmpty(fromId) && fig.Name.Contains(fromId))
                                 || (!string.IsNullOrEmpty(toId)   && fig.Name.Contains(toId))
                                 || fig.Name == run.Id;
        }
        RefreshLiveViewer();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private ShellViewModel? GetShellViewModel()
    {
        var parent = VisualTreeHelper.GetParent(this);
        while (parent != null && parent is not ShellWindow)
            parent = VisualTreeHelper.GetParent(parent);
        return (parent as ShellWindow)?.DataContext as ShellViewModel;
    }

    // ── Close Workspace ───────────────────────────────────────────────────────

    private void BtnCloseWorkspace_Click(object sender, RoutedEventArgs e)
    {
        var parent = VisualTreeHelper.GetParent(this);
        while (parent != null && parent is not ShellWindow)
            parent = VisualTreeHelper.GetParent(parent);

        if (parent is ShellWindow shell && shell.DataContext is ShellViewModel vm)
            vm.SelectedTabIndex = 0;
    }
}
