using System.Collections.ObjectModel;
using System.Windows.Input;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.ViewModels;

// ── Step status shown in the navigator ───────────────────────────────────────

public enum NavStepStatus { NotStarted, InProgress, Complete, Warning, Blocked }

public class WorkflowStepVm : ViewModelBase
{
    private NavStepStatus _status = NavStepStatus.NotStarted;
    private bool       _isSelected;
    private int        _errorCount;
    private int        _warningCount;

    public WorkflowPhase Phase        { get; init; }
    public string        StepNumber   { get; init; } = string.Empty;
    public string        Label        { get; init; } = string.Empty;
    public string        Icon         { get; init; } = string.Empty;

    // Computed badge colour key (consumed by XAML trigger)
    public string StatusKey => Status switch
    {
        NavStepStatus.Complete    => "Complete",
        NavStepStatus.Warning     => "Warning",
        NavStepStatus.Blocked     => "Blocked",
        NavStepStatus.InProgress  => "InProgress",
        _                      => "NotStarted"
    };

    public string StatusGlyph => Status switch
    {
        NavStepStatus.Complete   => "✅",
        NavStepStatus.Warning    => "⚠",
        NavStepStatus.Blocked    => "🔴",
        NavStepStatus.InProgress => "⚙",
        _                     => "⬜"
    };

    public NavStepStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusKey)); OnPropertyChanged(nameof(StatusGlyph)); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public int ErrorCount
    {
        get => _errorCount;
        set { _errorCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasIssues)); }
    }

    public int WarningCount
    {
        get => _warningCount;
        set { _warningCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasIssues)); }
    }

    public bool HasIssues => ErrorCount > 0 || WarningCount > 0;

    public ICommand? SelectCommand { get; set; }
}

// ── Navigator ViewModel ───────────────────────────────────────────────────────

public class WorkflowNavigatorViewModel : ViewModelBase
{
    private WorkflowPhase _currentPhase = WorkflowPhase.Intake;
    private string        _jobLabel     = "No Job Open";
    private string        _revLabel     = string.Empty;

    public ObservableCollection<WorkflowStepVm> Steps { get; } = [];

    public string JobLabel
    {
        get => _jobLabel;
        set { _jobLabel = value; OnPropertyChanged(); }
    }

    public string RevLabel
    {
        get => _revLabel;
        set { _revLabel = value; OnPropertyChanged(); }
    }

    public WorkflowPhase CurrentPhase
    {
        get => _currentPhase;
        set
        {
            _currentPhase = value;
            foreach (var s in Steps)
                s.IsSelected = s.Phase == value;
            OnPropertyChanged();
        }
    }

    public WorkflowNavigatorViewModel()
    {
        foreach (var info in WorkflowManager.Steps)
        {
            Steps.Add(new WorkflowStepVm
            {
                Phase      = info.Phase,
                StepNumber = info.StepNumber,
                Label      = info.Label,
                Icon       = info.Icon,
                Status     = NavStepStatus.NotStarted
            });
        }
        // Mark Intake as in-progress by default
        Steps[0].Status = NavStepStatus.InProgress;
        CurrentPhase = WorkflowPhase.Intake;
    }

    /// <summary>
    /// Refreshes all step statuses from the current job and validation result.
    /// Call this after every significant user action that mutates job state.
    /// </summary>
    public void Refresh(AsBuiltJob job, ValidationResult validation)
    {
        var manager = new WorkflowManager();

        JobLabel = job.Identity.JobNumber.Length > 0
            ? job.Identity.JobNumber
            : (job.Identity.ClientName.Length > 0 ? job.Identity.ClientName : "Untitled Job");

        RevLabel = $"Rev {job.Identity.RevisionNumber}";

        foreach (var step in Steps)
        {
            var errors   = validation.Issues.Count(i => i.Severity == IssueSeverity.Error   && IssueMatchesPhase(i.Category, step.Phase));
            var warnings = validation.Issues.Count(i => i.Severity == IssueSeverity.Warning && IssueMatchesPhase(i.Category, step.Phase));
            step.ErrorCount   = errors;
            step.WarningCount = warnings;

            var blockers = manager.GetTransitionBlockers(job, step.Phase);

            step.Status = step.Phase switch
            {
                // Phases before current → mark complete if no errors
                _ when step.Phase < _currentPhase && errors == 0   => NavStepStatus.Complete,
                _ when step.Phase < _currentPhase && errors > 0    => NavStepStatus.Warning,
                // Current phase
                _ when step.Phase == _currentPhase                  => NavStepStatus.InProgress,
                // Future phases
                _ when blockers.Count > 0                           => NavStepStatus.Blocked,
                _ when warnings > 0                                 => NavStepStatus.Warning,
                _                                                   => NavStepStatus.NotStarted
            };
        }
    }

    // Maps IssueCategory → the WorkflowPhase responsible for it
    private static bool IssueMatchesPhase(IssueCategory category, WorkflowPhase phase) =>
        (category, phase) switch
        {
            (IssueCategory.Geometry,      WorkflowPhase.Structures) => true,
            (IssueCategory.Coordinates,   WorkflowPhase.PointsCleanup) => true,
            (IssueCategory.Structures,    WorkflowPhase.Structures) => true,
            (IssueCategory.Runs,          WorkflowPhase.PipeRuns) => true,
            (IssueCategory.PartsMapping,  WorkflowPhase.PartsMapping) => true,
            (IssueCategory.Labels,        WorkflowPhase.Preview) => true,
            (IssueCategory.Projection,    WorkflowPhase.Intake) => true,
            (IssueCategory.ExportReadiness, WorkflowPhase.Deliverables) => true,
            _ => false
        };
}
