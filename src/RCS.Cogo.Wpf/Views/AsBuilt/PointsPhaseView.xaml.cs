using System.Windows;
using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class PointsPhaseView : UserControl
{
    private AsBuiltWorkspaceViewModel? Vm => DataContext as AsBuiltWorkspaceViewModel;

    public PointsPhaseView() => InitializeComponent();

    /// <summary>Bind the points grid to a flat observable list derived from the job's point collection.</summary>
    public void Load(AsBuiltJob job)
    {
        PointsGrid.ItemsSource = job.PointRows;
        TxtPointCount.Text = $"{job.PointRows.Count} points";
    }

    private void BtnAddPoint_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        var row = new PointRow { PointId = $"P{Vm.Job.PointRows.Count + 1}" };
        Vm.Job.PointRows.Add(row);
        TxtPointCount.Text = $"{Vm.Job.PointRows.Count} points";
        Vm.RequestRevalidation();
    }

    private void BtnDeletePoint_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        var selected = PointsGrid.SelectedItems.Cast<PointRow>().ToList();
        foreach (var r in selected) Vm.Job.PointRows.Remove(r);
        TxtPointCount.Text = $"{Vm.Job.PointRows.Count} points";
        Vm.RequestRevalidation();
    }

    private void BtnMergeDuplicates_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        var dupes = Vm.Job.PointRows
            .GroupBy(p => p.PointId.Trim())
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();
        foreach (var d in dupes) Vm.Job.PointRows.Remove(d);
        TxtPointCount.Text = $"{Vm.Job.PointRows.Count} points ({dupes.Count} merged)";
        Vm.RequestRevalidation();
    }

    private void PointsGrid_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (PointsGrid.SelectedItem is PointRow row)
            Vm?.OnCanvasPointSelected(row.PointId);
    }

    private void BtnFindDuplicates_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        const double thr = 0.01;
        var dupeIds = Vm.Job.PointRows
            .GroupBy(p => (Math.Round(p.Northing / thr), Math.Round(p.Easting / thr)))
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(r => r.PointId))
            .ToHashSet();

        // Highlight via row style — requires PointRow to expose IsDuplicate flag
        // Currently this is a code-behind highlight; full binding requires PointRow.IsDuplicate
        var dupeCount = dupeIds.Count;
        TxtDupeCount.Text = dupeCount > 0 ? $"⚠ {dupeCount} near-duplicates" : "✔ No duplicates";
    }

    private void BtnAutoFixDesc_Click(object s, RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        int fixed_ = 0;
        foreach (var r in Vm.Job.PointRows)
        {
            var norm = r.Description?.Trim().ToUpper() ?? string.Empty;
            if (norm != r.Description) { r.Description = norm; fixed_++; }
        }
        PointsGrid.Items.Refresh();
        TxtPointCount.Text = $"{Vm.Job.PointRows.Count} points ({fixed_} descriptions normalised)";
    }

    private void TxtFilter_TextChanged(object s, TextChangedEventArgs e)
    {
        var filter = TxtFilter.Text.Trim();
        if (Vm?.Job == null) return;
        if (string.IsNullOrWhiteSpace(filter))
        {
            PointsGrid.ItemsSource = Vm.Job.PointRows;
            return;
        }
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Vm.Job.PointRows);
        view.Filter = o => o is PointRow row
            && (row.PointId.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || (row.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) ?? false));
        PointsGrid.ItemsSource = view;
    }
}
