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
    /// Commands the canvas to pan/highlight the matching glyph.
    /// </summary>
    private void OnStructureSelectionChanged(object? sender,
        RCS.Piping.Core.Models.PipeStructure? structure)
    {
        if (structure == null || _injectedCanvas == null) return;
        // TODO: walk _injectedCanvas.Children, find the glyph by PointId tag,
        //       apply a highlight effect (stroke color / scale animation).
    }

    /// <summary>
    /// Called when the ViewModel selects a pipe run via the data grid.
    /// Commands the canvas to bold-highlight the matching pipe line.
    /// </summary>
    private void OnRunSelectionChanged(object? sender,
        RCS.Piping.Core.Models.PipeRun? run)
    {
        if (run == null || _injectedCanvas == null) return;
        // TODO: walk _injectedCanvas.Children, find the polyline by run tag,
        //       apply highlight stroke.
    }

    // ── Canvas → VM selection sync ───────────────────────────────────────────
    // When the canvas fires a mouse-click on a structure glyph, call:
    //   _vm?.OnCanvasStructureClicked(structureId);
    // This updates SelectedStructure on the VM which cascades to the
    // data grid via TwoWay binding.

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
