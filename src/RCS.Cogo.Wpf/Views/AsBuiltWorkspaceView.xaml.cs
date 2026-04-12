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
    /// Draws all pipe runs (lines) and structures (ellipses) into <see cref="_liveCanvas"/>
    /// using a bounding-box auto-fit so every network element is always visible.
    /// Color is resolved per utility type, matching the DxfBuilder layer palette.
    /// </summary>
    private void RefreshLiveViewer()
    {
        _liveCanvas.Children.Clear();

        var shellVm = GetShellViewModel();
        if (shellVm == null || shellVm.Points.Count == 0)
        {
            _watermark.Visibility = Visibility.Visible;
            return;
        }

        _watermark.Visibility = Visibility.Collapsed;

        // ── 1. Compute world bounding box ─────────────────────────────────────
        double minN = shellVm.Points.Min(p => p.Northing);
        double maxN = shellVm.Points.Max(p => p.Northing);
        double minE = shellVm.Points.Min(p => p.Easting);
        double maxE = shellVm.Points.Max(p => p.Easting);

        double worldW = maxE - minE; if (worldW < 1e-6) worldW = 1;
        double worldH = maxN - minN; if (worldH < 1e-6) worldH = 1;

        double sw = _liveCanvas.ActualWidth  > 10 ? _liveCanvas.ActualWidth  : 500;
        double sh = _liveCanvas.ActualHeight > 10 ? _liveCanvas.ActualHeight : 400;

        const double padPx = 24;
        double scale = Math.Min((sw - padPx * 2) / worldW, (sh - padPx * 2) / worldH);
        double ox    = padPx + (sw - padPx * 2 - worldW * scale) / 2.0;
        double oy    = padPx + (sh - padPx * 2 - worldH * scale) / 2.0;

        double Sx(double e) => ox + (e - minE) * scale;
        double Sy(double n) => sh - (oy + (n - minN) * scale);   // flip Y

        // ── 2. Draw pipe runs ─────────────────────────────────────────────────
        var ptDict = shellVm.Points.ToDictionary(p => p.Id, p => p);

        foreach (var run in shellVm.PipeRuns)
        {
            if (!ptDict.TryGetValue(run.FromPointId, out var fp)) continue;
            if (!ptDict.TryGetValue(run.ToPointId,   out var tp)) continue;

            _liveCanvas.Children.Add(new Line
            {
                X1 = Sx(fp.Easting), Y1 = Sy(fp.Northing),
                X2 = Sx(tp.Easting), Y2 = Sy(tp.Northing),
                Stroke          = ResolveRunBrush(run.Type),
                StrokeThickness = 1.8,
                ToolTip         = $"{run.Type}  Ø{run.Diameter}\" {run.Material}  {run.FromPointId}→{run.ToPointId}"
            });
        }

        // ── 3. Draw structure glyphs ──────────────────────────────────────────
        foreach (var sg in shellVm.StructureGraphics)
        {
            double cx = Sx(sg.Easting), cy = Sy(sg.Northing);
            double r  = sg.IsSelected ? 7 : 5;

            var ring = new Ellipse
            {
                Width           = r * 2, Height = r * 2,
                Stroke          = sg.IsSelected ? Brushes.Gold : Brushes.DimGray,
                Fill            = sg.IsSelected
                    ? new SolidColorBrush(Color.FromArgb(120, 255, 215,  0))
                    : new SolidColorBrush(Color.FromArgb(180,  40,  60, 80)),
                StrokeThickness = sg.IsSelected ? 2.5 : 1.2,
                ToolTip         = $"{sg.Type}  [{sg.Id}]"
            };
            Canvas.SetLeft(ring, cx - r);
            Canvas.SetTop(ring,  cy - r);
            _liveCanvas.Children.Add(ring);
        }

        // ── 4. Point ID labels (omit when >200 points for perf) ─────────────
        if (shellVm.Points.Count <= 200)
        {
            foreach (var pt in shellVm.Points)
            {
                var lbl = new TextBlock
                {
                    Text       = pt.Id,
                    FontSize   = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7A, 0x9E, 0xCE)),
                    FontFamily = new FontFamily("Segoe UI")
                };
                Canvas.SetLeft(lbl, Sx(pt.Easting) + 5);
                Canvas.SetTop(lbl,  Sy(pt.Northing) - 10);
                _liveCanvas.Children.Add(lbl);
            }
        }
    }

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

    // ── DataContext Wiring ────────────────────────────────────────────────────

    private void OnDataContextChanged(object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
        {
            _vm.StructureSelectionChanged -= OnStructureSelectionChanged;
            _vm.RunSelectionChanged       -= OnRunSelectionChanged;
        }

        _vm = e.NewValue as AsBuiltWorkspaceViewModel;
        if (_vm == null) return;

        _vm.StructureSelectionChanged += OnStructureSelectionChanged;
        _vm.RunSelectionChanged       += OnRunSelectionChanged;
    }

    private void OnStructureSelectionChanged(object? sender,
        RCS.Piping.Core.Models.PipeStructure? structure)
    {
        if (structure == null) return;

        var shellVm = GetShellViewModel();
        if (shellVm == null) return;

        StructureViewModel? match = null;
        foreach (var sg in shellVm.StructureGraphics)
        {
            sg.IsSelected = (sg.Id == structure.PointId);
            if (sg.IsSelected) match = sg;
        }

        RefreshLiveViewer();

        if (match != null)
            PanCanvasToWorld(shellVm, match.Easting, match.Northing);
    }

    private void OnRunSelectionChanged(object? sender,
        RCS.Piping.Core.Models.PipeRun? run)
    {
        if (run == null) return;

        var shellVm = GetShellViewModel();
        if (shellVm == null) return;

        string fromId = run.FromPointId ?? "";
        string toId   = run.ToPointId   ?? "";

        foreach (var fig in shellVm.Figures)
        {
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

    private static void PanCanvasToWorld(ShellViewModel shellVm, double worldX, double worldY)
        => shellVm.ZoomExtentsCommand?.Execute(null);

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
