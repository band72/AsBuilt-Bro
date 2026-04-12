using System.Collections.ObjectModel;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.ViewModels;

// ── Audit trail entry ────────────────────────────────────────────────────────

public class AuditEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string   Message   { get; init; } = string.Empty;

    public static AuditEntry Create(string message)
        => new() { Timestamp = DateTime.Now, Message = message };
}

// ── DiagnosticsViewModel ─────────────────────────────────────────────────────

/// <summary>
/// Drives the DiagnosticsPane bottom dock.
/// Bound to the same ValidationResult that the WorkflowNavigator uses.
/// </summary>
public class DiagnosticsViewModel : ViewModelBase
{
    private string _outputLog = string.Empty;
    private int    _errorCount;

    public ObservableCollection<ValidationIssue> Errors   { get; } = [];
    public ObservableCollection<ValidationIssue> Warnings { get; } = [];
    public ObservableCollection<AuditEntry>      AuditEntries { get; } = [];

    public string OutputLog
    {
        get => _outputLog;
        set { _outputLog = value; OnPropertyChanged(); }
    }

    public int ErrorCount
    {
        get => _errorCount;
        set { _errorCount = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Refresh Errors and Warnings lists from a fresh ValidationResult.
    /// Call from the same refresh path as WorkflowNavigatorViewModel.Refresh().
    /// </summary>
    public void Refresh(ValidationResult result)
    {
        Errors.Clear();
        Warnings.Clear();

        foreach (var issue in result.Issues.OrderBy(i => i.Category))
        {
            if (issue.Severity == IssueSeverity.Error)
                Errors.Add(issue);
            else if (issue.Severity == IssueSeverity.Warning)
                Warnings.Add(issue);
        }

        ErrorCount = Errors.Count;
    }

    /// <summary>Append a line to the output log (Info tab).</summary>
    public void AppendLog(string line)
    {
        OutputLog += $"[{DateTime.Now:HH:mm:ss}] {line}\n";
        OnPropertyChanged(nameof(OutputLog));
    }

    /// <summary>Record a timestamped user/system action in the audit trail.</summary>
    public void Audit(string message)
    {
        AuditEntries.Insert(0, AuditEntry.Create(message));
        // Keep trail bounded
        while (AuditEntries.Count > 200)
            AuditEntries.RemoveAt(AuditEntries.Count - 1);
    }
}
