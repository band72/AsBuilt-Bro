using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RCS.Cogo.Wpf.ViewModels;
using WorkflowResult = RCS.Piping.Core.Workflow.ValidationResult;
using WorkflowIssue  = RCS.Piping.Core.Workflow.ValidationIssue;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.Views.AsBuilt;

public partial class ValidationPhaseView : UserControl
{
    private AsBuiltWorkspaceViewModel? Vm => DataContext as AsBuiltWorkspaceViewModel;
    private IssueCategory? _activeFilter;

    public ValidationPhaseView() => InitializeComponent();

    /// <summary>Populate the issue list from an external ValidationResult.</summary>
    public void Load(RCS.Piping.Core.Workflow.ValidationResult result)

    {
        TxtErrorCount.Text = result.ErrorCount.ToString();
        TxtWarnCount.Text  = result.WarningCount.ToString();
        int passed = 5 - result.Issues.Select(i => i.RuleName).Distinct().Count();  // rules run = 5
        TxtPassCount.Text  = Math.Max(0, passed).ToString();

        bool exportReady = result.IsExportReady;
        TxtExportGate.Text           = exportReady ? "✅  Export Ready" : "🔴  Export Blocked";
        TxtExportGate.Foreground     = new System.Windows.Media.SolidColorBrush(
            exportReady ? System.Windows.Media.Color.FromRgb(0x2E, 0xCC, 0x71)
                        : System.Windows.Media.Color.FromRgb(0xE7, 0x4C, 0x3C));
        ExportGateBadge.Background   = new System.Windows.Media.SolidColorBrush(
            exportReady ? System.Windows.Media.Color.FromRgb(0x1A, 0x4D, 0x2E)
                        : System.Windows.Media.Color.FromRgb(0x4D, 0x1A, 0x1A));

        ApplyFilter(result.Issues);
    }

    private void ApplyFilter(IEnumerable<ValidationIssue> allIssues)
    {
        var filtered = _activeFilter.HasValue
            ? allIssues.Where(i => i.Category == _activeFilter.Value).ToList()
            : allIssues.ToList();
        IssueList.ItemsSource = filtered;
    }

    private void BtnRunValidation_Click(object s, RoutedEventArgs e)
        => Vm?.ValidateNowCommand?.Execute(null);

    private void FilterBtn_Click(object s, RoutedEventArgs e)
    {
        if (s is not Button btn) return;
        var tag = btn.Tag?.ToString();
        _activeFilter = tag == "All" || string.IsNullOrEmpty(tag)
            ? null
            : Enum.TryParse<IssueCategory>(tag, out var cat) ? cat : (IssueCategory?)null;
        if (Vm != null) Load(Vm.LastValidation);
    }

    private void IssueList_SelectionChanged(object s, SelectionChangedEventArgs e) { }

    private void BtnZoomTo_Click(object s, RoutedEventArgs e)
    {
        if (s is Button btn && btn.Tag is string id)
            Vm?.OnCanvasStructureClicked(id);   // generalised — canvas zooms to structure or run
    }

    private void BtnAutoFix_Click(object s, RoutedEventArgs e)
    {
        if (s is not Button btn || btn.Tag is not ValidationIssue issue) return;
        // Dispatch to VM for phase-aware auto-fix logic
        Vm?.ApplyAutoFix(issue);
    }
    private void BtnAutoFixAll_Click(object s, RoutedEventArgs e)
    {
        if (Vm == null) return;
        var fixable = Vm.LastValidation.Issues.Where(i => i.AutoFixable).ToList();
        foreach (var issue in fixable)
            Vm.ApplyAutoFix(issue);
        Vm.ValidateNowCommand?.Execute(null);   // re-validate after fixes
    }

    private void BtnCopyReport_Click(object s, RoutedEventArgs e)
    {
        if (Vm == null) return;
        var lines = Vm.LastValidation.Issues
            .Select(i => $"[{i.Severity}] {i.Category} | {i.RuleName} — {i.Message}" +
                         (string.IsNullOrWhiteSpace(i.SuggestedFix) ? "" : $"\n  Fix: {i.SuggestedFix}"));
        Clipboard.SetText(string.Join(Environment.NewLine + Environment.NewLine, lines));
    }
}
