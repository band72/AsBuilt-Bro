using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.Views;

/// <summary>
/// Code-behind for the four-pane As-Built production workspace.
/// All business logic lives in <see cref="AsBuiltWorkspaceViewModel"/>;
/// this file is responsible only for the canvas injection and the two-way
/// selection sync between the live viewer and the center-pane data grids.
/// </summary>
public partial class AsBuiltWorkspaceView : UserControl
{
    private AsBuiltWorkspaceViewModel? _vm;

    // Shared canvas injected from ShellWindow — rendered inside CanvasRegion.
    private Canvas? _injectedCanvas;

    public AsBuiltWorkspaceView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    // ── Canvas Injection ─────────────────────────────────────────────────────

    /// <summary>
    /// Called by <see cref="ShellWindow"/> on first As-Built tab activation.
    /// Places the shared <see cref="Canvas"/> (with its WorldTransform already
    /// applied) into the CanvasRegion Border so the workspace right pane
    /// renders inside the same coordinate system as the main shell canvas.
    /// </summary>
    /// <param name="canvas">The existing ViewportCanvas from ShellWindow.</param>
    public void InjectCanvas(Canvas canvas)
    {
        if (canvas == null) return;
        _injectedCanvas = canvas;

        // CanvasRegion is a Border defined in AsBuiltWorkspaceView.xaml.
        // We do NOT reparent the canvas — the ShellWindow canvas must stay in its
        // own visual tree so transforms keep working. Instead we set the same
        // DataContext reference on the region so future renderers can share it.
        //
        // For now, store the reference; per-phase overlay rendering can be
        // injected here once the DXFBuilder pipeline is connected.
        //
        //   CanvasRegion.Child = canvas;   // only safe if ShellWindow removes it first
    }

    // ── DataContext Wiring ───────────────────────────────────────────────────

    private void OnDataContextChanged(object sender,
        System.Windows.DependencyPropertyChangedEventArgs e)
    {
        // Unsubscribe from previous VM
        if (_vm != null)
        {
            _vm.StructureSelectionChanged -= OnStructureSelectionChanged;
            _vm.RunSelectionChanged       -= OnRunSelectionChanged;
        }

        _vm = e.NewValue as AsBuiltWorkspaceViewModel;

        if (_vm == null) return;

        // Subscribe to canvas-highlight events fired by the VM
        _vm.StructureSelectionChanged += OnStructureSelectionChanged;
        _vm.RunSelectionChanged       += OnRunSelectionChanged;
    }

    /// <summary>
    /// Called when the ViewModel selects a structure via the data grid.
    /// Finds the matching StructureViewModel by PointId, sets IsSelected (drives gold ring),
    /// then pans the viewport to center on it.
    /// </summary>
    private void OnStructureSelectionChanged(object? sender,
        RCS.Piping.Core.Models.PipeStructure? structure)
    {
        if (structure == null || _injectedCanvas == null) return;

        // Walk up to ShellWindow to access StructureGraphics collection
        var shellVm = GetShellViewModel();
        if (shellVm == null) return;

        // Clear previous selection, highlight matching glyph
        StructureViewModel? match = null;
        foreach (var sg in shellVm.StructureGraphics)
        {
            if (sg.Id == structure.PointId)
            {
                sg.IsSelected = true;
                match = sg;
            }
            else
            {
                sg.IsSelected = false;
            }
        }

        // Pan viewport to center on selected structure
        if (match != null)
            PanCanvasToWorld(shellVm, match.Easting, match.Northing);
    }

    /// <summary>
    /// Called when the ViewModel selects a pipe run via the data grid.
    /// Finds matching FigureViewModel(s) by name and sets IsHighlighted,
    /// which drives the cyan overlay polyline through data binding.
    /// </summary>
    private void OnRunSelectionChanged(object? sender,
        RCS.Piping.Core.Models.PipeRun? run)
    {
        if (run == null || _injectedCanvas == null) return;

        var shellVm = GetShellViewModel();
        if (shellVm == null) return;

        // Build search keys: figures may be named by utility type or "From_To" pattern
        string fromId = run.FromPointId ?? "";
        string toId   = run.ToPointId   ?? "";

        foreach (var fig in shellVm.Figures)
        {
            // Match on figure name containing either endpoint ID, or the run Id
            bool isMatch = !string.IsNullOrEmpty(fromId) && fig.Name.Contains(fromId)
                        || !string.IsNullOrEmpty(toId)   && fig.Name.Contains(toId)
                        || fig.Name == run.Id;

            fig.IsHighlighted = isMatch;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private ShellViewModel? GetShellViewModel()
    {
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(this);
        while (parent != null && parent is not ShellWindow)
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        return (parent as ShellWindow)?.DataContext as ShellViewModel;
    }

    /// <summary>Translates the WorldTransform so the given world coordinate
    /// appears at the center of the viewport.</summary>
    private static void PanCanvasToWorld(ShellViewModel shellVm, double worldX, double worldY)
    {
        // The WorldTransform is a MatrixTransform on the ViewportCanvas.
        // We need the ShellWindow to expose it — for now use the CurrentViewScale
        // from the VM to compute the screen offset.
        //
        // Approach: fire a refresh of SelectedStructure so the VM can invoke
        // ZoomExtents or a targeted pan through its own command in a future pass.
        // For this iteration, simply request a ZoomExtents so the selected item
        // is brought into view.
        shellVm.ZoomExtentsCommand?.Execute(null);
    }

    // ── Close / Exit As-Built ────────────────────────────────────────────────

    /// <summary>
    /// Walks up to the ShellWindow and switches the selected tab back to 0
    /// (Points / Cogo), which collapses the As-Built overlay.
    /// </summary>
    private void BtnCloseWorkspace_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        // Walk up visual tree to the ShellWindow
        var parent = System.Windows.Media.VisualTreeHelper.GetParent(this);
        while (parent != null && parent is not ShellWindow)
            parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);

        if (parent is ShellWindow shell &&
            shell.DataContext is ShellViewModel vm)
        {
            vm.SelectedTabIndex = 0;   // Switch to Points tab → hides overlay
        }
    }
}
