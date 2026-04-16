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

    private void BtnGraphProfile_Click(object s, System.Windows.RoutedEventArgs e)
    {
        if (Vm?.Job == null || RunsGrid.SelectedItem is not PipeRun run) return;
        var win = new ProfileVisualizerWindow(Vm.Job, run) { Owner = System.Windows.Application.Current.MainWindow };
        win.ShowDialog();
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
        var runs = Vm.Job.Network.GetAllRuns().ToList();
        if (runs.Count == 0) return;

        // Reset stationing
        foreach(var r in runs) r.PartKey = "";

        // Find the most upstream point (source) by finding a FromPointId that is never a ToPointId
        var toPoints = runs.Select(r => r.ToPointId).ToHashSet();
        var sources = runs.Where(r => !toPoints.Contains(r.FromPointId)).ToList();
        
        if (sources.Count == 0) sources = new List<RCS.Piping.Core.Models.PipeRun> { runs[0] };

        double totalStationing = 0;
        int chained = 0;
        var finalOrder = new System.Collections.Generic.List<RCS.Piping.Core.Models.PipeRun>();
        var visited = new System.Collections.Generic.HashSet<string>();

        // DFS Traversal
        foreach(var source in sources)
        {
            var stack = new System.Collections.Generic.Stack<RCS.Piping.Core.Models.PipeRun>();
            stack.Push(source);
            
            while(stack.Count > 0)
            {
                var cur = stack.Pop();
                if (visited.Contains(cur.Id)) continue;
                
                visited.Add(cur.Id);
                finalOrder.Add(cur);
                
                double runLen = cur.ComputedLength > 0 ? cur.ComputedLength : 10.0;
                cur.PartKey = $"STA {Math.Floor(totalStationing / 100):00}+{totalStationing % 100:00.00} to {Math.Floor((totalStationing + runLen) / 100):00}+{(totalStationing + runLen) % 100:00.00}";
                totalStationing += runLen;
                chained++;

                // find children (laterals pushed first, mainlines last so they pop first)
                var children = runs.Where(r => r.FromPointId == cur.ToPointId && !visited.Contains(r.Id)).ToList();
                foreach(var child in children) stack.Push(child);
            }
        }
        
        // Unconnected stragglers
        foreach(var r in runs.Where(x => !visited.Contains(x.Id))) finalOrder.Add(r);
        
        // Re-write dictionary in order
        Vm.Job.Network.Runs.Clear();
        foreach(var r in finalOrder) Vm.Job.Network.Runs[r.Id] = r;
        
        Vm.Job.AuditLog.Add(new RCS.Piping.Core.Workflow.AuditEntry { Action = "Auto-Chain Executed", Details = $"Dendritic DFS Pathfinding applied to {chained} runs" });

        TxtRunDetail.Text = chained > 0
            ? $"✅ Auto-chained {chained} run(s). Dendritic Stationing established."
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
