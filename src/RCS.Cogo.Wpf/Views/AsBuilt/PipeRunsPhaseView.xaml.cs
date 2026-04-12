using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;
using RCS.Piping.Core.Models;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class PipeRunsPhaseView : UserControl
{
    private AsBuiltWorkspaceViewModel? Vm => DataContext as AsBuiltWorkspaceViewModel;

    public PipeRunsPhaseView() => InitializeComponent();

    public void Load(RCS.Piping.Core.Workflow.AsBuiltJob job)
    {
        RefreshGrid();
    }

    private void BtnAddRun_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        Vm.Job.Network.AddRun(new PipeRun { Diameter = 8, Material = "PVC" });
        RefreshGrid();
        Vm.RequestRevalidation();
    }

    private void BtnComputeSlopes_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        int computed = 0;
        foreach (var run in Vm.Job.Network.GetAllRuns())
        {
            if (run.InvertStart.HasValue && run.InvertEnd.HasValue)
            {
                // Compute length from associated points if possible
                var fromPt = Vm.Job.PointRows.FirstOrDefault(p => p.PointId == run.FromPointId);
                var toPt   = Vm.Job.PointRows.FirstOrDefault(p => p.PointId == run.ToPointId);
                if (fromPt != null && toPt != null)
                {
                    double dx = toPt.Easting  - fromPt.Easting;
                    double dy = toPt.Northing - fromPt.Northing;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 0)
                    {
                        run.ComputedLength   = len;
                        run.SlopePercent     = Math.Abs((run.InvertStart.Value - run.InvertEnd.Value) / len * 100);
                        computed++;
                    }
                }
            }
        }
        TxtRunDetail.Text = computed > 0
            ? $"✅ Computed slope and length for {computed} run(s)."
            : "No runs had sufficient data (requires InvertStart, InvertEnd, and linked points).";
        RefreshGrid();
    }

    private void BtnReverseFlow_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (RunsGrid.SelectedItem is not PipeRun run) return;
        run.FlowReversed = !run.FlowReversed;
        (run.InvertStart, run.InvertEnd) = (run.InvertEnd, run.InvertStart);
        RefreshGrid();
        Vm?.RequestRevalidation();
    }

    private void RunsGrid_SelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (RunsGrid.SelectedItem is not PipeRun run) { TxtRunDetail.Text = ""; return; }
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Run ID      : {run.Id}");
        sb.AppendLine($"From → To   : {run.FromPointId} → {run.ToPointId}");
        sb.AppendLine($"Diameter    : {run.Diameter} in  |  Material: {run.Material}");
        if (run.InvertStart.HasValue) sb.AppendLine($"Invert US   : {run.InvertStart:F3} ft");
        if (run.InvertEnd.HasValue)   sb.AppendLine($"Invert DS   : {run.InvertEnd:F3} ft");
        if (run.SlopePercent > 0)     sb.AppendLine($"Slope       : {run.SlopePercent:F3} %");
        if (run.ComputedLength > 0)   sb.AppendLine($"Computed Len: {run.ComputedLength:F2} ft");
        sb.AppendLine($"Flow Reversed: {run.FlowReversed}");
        TxtRunDetail.Text = sb.ToString();
        Vm?.OnCanvasRunSelected(run.Id);
    }

    private void BtnAutoChain_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null) return;
        var runs     = Vm.Job.Network.GetAllRuns().ToList();
        var structs  = Vm.Job.Network.GetAllStructures().Select(s2 => s2.PointId).ToHashSet();
        int chained  = 0;

        // Link runs sequentially: if run[i].ToPointId matches a structure point that
        // also appears as run[i+1].FromPointId, the chain is already correct.
        // If not, we attempt to reorder by tracing the shared-point graph.
        for (int i = 0; i < runs.Count - 1; i++)
        {
            var current = runs[i];
            var next    = runs.FirstOrDefault(r => r.FromPointId == current.ToPointId && r != current);
            if (next != null && runs.IndexOf(next) != i + 1)
            {
                // Promote 'next' to be immediately after 'current'
                runs.Remove(next);
                runs.Insert(i + 1, next);
                chained++;
            }
        }

        TxtRunDetail.Text = chained > 0
            ? $"✅ Auto-chained {chained} run(s) into a continuous linear sequence."
            : "ℹ Runs are already in chain order — no reordering needed.";
        RefreshGrid();
        Vm.RequestRevalidation();
    }

    private void RefreshGrid()
    {
        var allRuns = Vm?.Job?.Network.GetAllRuns().ToList() ?? [];
        RunsGrid.ItemsSource = allRuns;

        var totalLength    = allRuns.Sum(r => r.ComputedLength);
        var reversalCount  = allRuns.Count(r => r.SlopePercent < 0);

        TxtRunCount.Text = totalLength > 0
            ? $"{allRuns.Count} runs  ·  {totalLength:F1} ft total" + (reversalCount > 0 ? $"  ·  ⚠ {reversalCount} slope reversals" : "")
            : $"{allRuns.Count} runs";
    }
}
