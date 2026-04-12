using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;
using RCS.Piping.Core.Models;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class StructuresPhaseView : UserControl
{
    private AsBuiltWorkspaceViewModel? Vm => DataContext as AsBuiltWorkspaceViewModel;
    private PipeStructure? _selectedStructure;
    private bool _suppressDetailEvents;

    public StructuresPhaseView() => InitializeComponent();

    public void Load(RCS.Piping.Core.Workflow.AsBuiltJob job)
    {
        StructuresGrid.ItemsSource = job.Network.GetAllStructures().ToList();
        TxtStructCount.Text = $"{job.Network.Structures.Count} structures";
    }

    private void BtnAddStructure_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        var st = new PipeStructure { Id = $"MH-{Vm.Job.Network.Structures.Count + 1:D3}", Type = "Manhole" };
        Vm.Job.Network.AddStructure(st);
        RefreshGrid();
        Vm.RequestRevalidation();
    }

    private void BtnInferRims_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        int inferred = 0;
        foreach (var st in Vm.Job.Network.GetAllStructures().Where(s => !s.RimElevation.HasValue))
        {
            // Find closest point with an elevation as a proxy rim
            var nearest = Vm.Job.PointRows
                .Where(p => p.Elevation != 0)
                .OrderBy(p => Math.Abs(p.Northing - 0) + Math.Abs(p.Easting - 0)) // simplified
                .FirstOrDefault();
            if (nearest != null) { st.RimElevation = nearest.Elevation; inferred++; }
        }
        TxtInferStatus.Text = inferred > 0 ? $"✅ Inferred {inferred} rim elevation(s) from nearest survey point." : "No rims could be inferred — add survey points first.";
        RefreshGrid();
        Vm.RequestRevalidation();
    }

    private void StructuresGrid_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        _selectedStructure = StructuresGrid.SelectedItem as PipeStructure;
        PopulateDetail(_selectedStructure);
        if (_selectedStructure != null)
            Vm?.OnCanvasStructureClicked(_selectedStructure.Id);
    }

    private void PopulateDetail(PipeStructure? st)
    {
        _suppressDetailEvents = true;
        TxtDetailId.Text     = st?.Id              ?? string.Empty;
        TxtDetailRim.Text    = st?.RimElevation?.ToString("F2") ?? string.Empty;
        TxtDetailInvIn.Text  = st?.InvertIn?.ToString("F2")     ?? string.Empty;
        TxtDetailInvOut.Text = st?.InvertOut?.ToString("F2")    ?? string.Empty;
        var idx = st?.Type switch
        {
            "Manhole"     => 0, "Inlet"       => 1, "Catch Basin" => 2,
            "Cleanout"    => 3, "Valve"       => 4, "Meter"       => 5,
            "Fitting"     => 6, "Junction Box"=> 7, _             => 8
        };
        CboDetailType.SelectedIndex = idx;
        _suppressDetailEvents = false;
    }

    private void Detail_TextChanged(object s, TextChangedEventArgs e)  => ApplyDetailToModel();
    private void Detail_SelectionChanged(object s, SelectionChangedEventArgs e) => ApplyDetailToModel();

    private void BtnApplyDetail_Click(object s, System.Windows.RoutedEventArgs e) => ApplyDetailToModel();

    private void ApplyDetailToModel()
    {
        if (_suppressDetailEvents || _selectedStructure == null) return;
        _selectedStructure.Id   = TxtDetailId.Text.Trim();
        _selectedStructure.Type = (CboDetailType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Other";
        if (double.TryParse(TxtDetailRim.Text,    out var r))  _selectedStructure.RimElevation = r; else _selectedStructure.RimElevation = null;
        if (double.TryParse(TxtDetailInvIn.Text,  out var ii)) _selectedStructure.InvertIn     = ii; else _selectedStructure.InvertIn     = null;
        if (double.TryParse(TxtDetailInvOut.Text, out var io)) _selectedStructure.InvertOut    = io; else _selectedStructure.InvertOut    = null;
        Vm?.RequestRevalidation();
    }

    private void RefreshGrid()
    {
        StructuresGrid.ItemsSource = Vm?.Job?.Network.GetAllStructures().ToList();
        TxtStructCount.Text = $"{Vm?.Job?.Network.Structures.Count ?? 0} structures";
    }
}
