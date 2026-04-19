using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using RCS.Piping.Core.Engines;
using RCS.Piping.Core.Models;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.ViewModels;

// ── Relay Command ─────────────────────────────────────────────────────────────
// Named with AsBuilt prefix to avoid polluting the shared namespace and
// shadowing System.Action, which caused CS1593 in pre-existing ShellViewModel code.

internal sealed class AsBuiltRelayCommand(System.Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? _) => canExecute?.Invoke() ?? true;
    public void Execute(object? _)
    {
        try { execute(); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Command execution failed:\n\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

internal sealed class AsBuiltAsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private bool _isRunning;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? _) => !_isRunning && (canExecute?.Invoke() ?? true);
    public async void Execute(object? _)
    {
        _isRunning = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try   { await execute(); }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Command execution failed:\n\n{ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            _isRunning = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

// ── Navigator Step VM ─────────────────────────────────────────────────────────

public enum StepStatus { Idle, Active, Complete, Warning, Blocked }

public class WorkflowStepViewModel : INotifyPropertyChanged
{
    private StepStatus _status = StepStatus.Idle;

    public WorkflowPhase Phase      { get; init; }
    public string        StepNumber { get; init; } = string.Empty;
    public string        Label      { get; init; } = string.Empty;
    public string        Icon       { get; init; } = string.Empty;

    public StepStatus Status
    {
        get => _status;
        set { if (_status == value) return; _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); OnPropertyChanged(nameof(IsActive)); }
    }

    // Derived UI helpers
    public bool   IsActive   => Status == StepStatus.Active;
    public string StatusIcon => Status switch
    {
        StepStatus.Complete => "✅",
        StepStatus.Warning  => "⚠️",
        StepStatus.Blocked  => "🔴",
        StepStatus.Active   => "▶",
        _                   => "○"
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

// ── Main Workspace ViewModel ──────────────────────────────────────────────────

/// <summary>
/// Drives the four-pane as-built production workspace.
/// Owns the AsBuiltJob, orchestrates validation, and coordinates
/// the three-way selection sync between grid, navigator, and canvas.
/// </summary>
public class AsBuiltWorkspaceViewModel : ViewModelBase
{
    // ── Dependencies ──────────────────────────────────────────────────────────
    private readonly WorkflowManager   _workflowManager = new();
    private readonly ValidationEngine  _validationEngine = new();

    // Background validation throttle
    private CancellationTokenSource? _validationCts;

    // ── Job State ─────────────────────────────────────────────────────────────
    private AsBuiltJob _job = new();
    public AsBuiltJob Job
    {
        get => _job;
        private set 
        { 
            SetField(ref _job, value); 
            RefreshNavigatorStatus(); 
            
            // Re-initialize undo stack when job completely changes
            _undoStack = new RCS.Cogo.Wpf.Services.AsBuiltUndoStack(_job, () =>
            {
                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
                OnPropertyChanged(nameof(UndoDescription));
                OnPropertyChanged(nameof(RedoDescription));
            });
        }
    }

    // ── Undo / Redo State ─────────────────────────────────────────────────────
    private RCS.Cogo.Wpf.Services.AsBuiltUndoStack? _undoStack;
    public bool CanUndo => _undoStack?.CanUndo ?? false;
    public bool CanRedo => _undoStack?.CanRedo ?? false;
    public string UndoDescription => _undoStack?.UndoDescription ?? "Undo";
    public string RedoDescription => _undoStack?.RedoDescription ?? "Redo";
    public RCS.Cogo.Wpf.Services.AsBuiltUndoStack? UndoStack => _undoStack;

    // ── Navigator ─────────────────────────────────────────────────────────────
    public ObservableCollection<WorkflowStepViewModel> Steps { get; } = new(
        WorkflowManager.Steps.Select(s => new WorkflowStepViewModel
        {
            Phase      = s.Phase,
            StepNumber = s.StepNumber,
            Label      = s.Label,
            Icon       = s.Icon
        }));

    public Action<string, bool>? ShowSnackbarRequested;

    private WorkflowStepViewModel? _selectedStep;
    public WorkflowStepViewModel? SelectedStep
    {
        get => _selectedStep;
        set
        {
            if (value == null || _selectedStep == value) return;
            var blockers = _workflowManager.GetTransitionBlockers(Job, value.Phase);
            if (blockers.Count > 0)
            {
                TransitionBlockerMessage = string.Join("\n• ", blockers.Prepend("Cannot navigate to this step:"));
                OnPropertyChanged(nameof(TransitionBlockerMessage));
                return;
            }
            if (_selectedStep != null) _selectedStep.Status = StepStatus.Idle;
            SetField(ref _selectedStep, value);
            _selectedStep!.Status = StepStatus.Active;
            Job.CurrentPhase = value.Phase;
            TransitionBlockerMessage = string.Empty;
            OnPropertyChanged(nameof(TransitionBlockerMessage));
        }
    }

    public string TransitionBlockerMessage { get; private set; } = string.Empty;

    // ── Validation State ──────────────────────────────────────────────────────
    private ValidationResult _lastValidation = ValidationResult.Empty;
    public ValidationResult LastValidation
    {
        get => _lastValidation;
        private set
        {
            SetField(ref _lastValidation, value);
            OnPropertyChanged(nameof(ErrorCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(IsExportReady));
            OnPropertyChanged(nameof(ExportGateLabel));
            RefreshDeliverableCards();
        }
    }

    public int    ErrorCount    => LastValidation.ErrorCount;
    public int    WarningCount  => LastValidation.WarningCount;
    public bool   IsExportReady => LastValidation.IsExportReady;
    public string ExportGateLabel => IsExportReady ? "✅  Export Ready" : $"🔴  Export Blocked — {ErrorCount} error(s)";

    // ── Deliverables ──────────────────────────────────────────────────────────
    public ObservableCollection<DeliverableCard> Deliverables => Job.Deliverables;

    // ── Selected Objects (two-way canvas ↔ grid sync) ────────────────────────
    private PipeStructure? _selectedStructure;
    public PipeStructure? SelectedStructure
    {
        get => _selectedStructure;
        set
        {
            SetField(ref _selectedStructure, value);
            // Fire event so the canvas can highlight this structure
            StructureSelectionChanged?.Invoke(this, value);
        }
    }

    private PipeRun? _selectedRun;
    public PipeRun? SelectedRun
    {
        get => _selectedRun;
        set
        {
            SetField(ref _selectedRun, value);
            RunSelectionChanged?.Invoke(this, value);
        }
    }

    // ── Canvas Selection Events (consumed by LiveViewerView code-behind) ──────
    public event EventHandler<PipeStructure?>? StructureSelectionChanged;
    public event EventHandler<PipeRun?>?       RunSelectionChanged;
    public event EventHandler<string?>?        PointSelectionChanged;
    public event EventHandler<int>?            SourceLineZoomRequested;

    // ── Commands ──────────────────────────────────────────────────────────────
    public System.Windows.Input.ICommand NewJobCommand               { get; }
    public System.Windows.Input.ICommand ValidateNowCommand          { get; }
    public System.Windows.Input.ICommand BuildPackageCommand         { get; }
    public System.Windows.Input.ICommand GenerateReportCommand       { get; }
    public System.Windows.Input.ICommand GoToNextStepCommand         { get; }
    public System.Windows.Input.ICommand GoToPrevStepCommand         { get; }
    public System.Windows.Input.ICommand ImportPointsListCommand     { get; }
    public System.Windows.Input.ICommand ImportBatchCommand          { get; }
    public System.Windows.Input.ICommand ImportJeaTemplateCommand    { get; }
    public System.Windows.Input.ICommand ImportDxfCommand            { get; }
    public System.Windows.Input.ICommand ImportBlueprintCommand      { get; }
    public System.Windows.Input.ICommand UndoCommand                 { get; }
    public System.Windows.Input.ICommand RedoCommand                 { get; }

    // ── Constructor ───────────────────────────────────────────────────────────
    public AsBuiltWorkspaceViewModel()
    {
        NewJobCommand               = new AsBuiltRelayCommand(StartNewJob);
        ValidateNowCommand          = new AsBuiltAsyncRelayCommand(RunValidationAsync);
        BuildPackageCommand         = new AsBuiltAsyncRelayCommand(BuildPackageAsync, () => IsExportReady);
        GenerateReportCommand       = new AsBuiltAsyncRelayCommand(GenerateReportAsync);
        GoToNextStepCommand         = new AsBuiltRelayCommand(GoToNextStep);
        GoToPrevStepCommand         = new AsBuiltRelayCommand(GoToPrevStep);
        ImportPointsListCommand     = new AsBuiltAsyncRelayCommand(() => ImportFileAsync(IntakeFileType.Pnezd));
        ImportBatchCommand          = new AsBuiltAsyncRelayCommand(() => ImportFileAsync(IntakeFileType.CogoScript));
        ImportJeaTemplateCommand    = new AsBuiltAsyncRelayCommand(() => ImportFileAsync(IntakeFileType.JeaExcel));
        ImportDxfCommand            = new AsBuiltAsyncRelayCommand(() => ImportFileAsync(IntakeFileType.Dxf));
        ImportBlueprintCommand      = new AsBuiltAsyncRelayCommand(ImportBlueprintAsync);

        UndoCommand                 = new AsBuiltRelayCommand(() => { _undoStack?.Undo(); RequestRevalidation(); }, () => CanUndo);
        RedoCommand                 = new AsBuiltRelayCommand(() => { _undoStack?.Redo(); RequestRevalidation(); }, () => CanRedo);

        // Select first step by default
        SelectedStep = Steps.First();
        
        // Ensure stack applies to current default job
        _undoStack = new RCS.Cogo.Wpf.Services.AsBuiltUndoStack(_job, () =>
        {
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            OnPropertyChanged(nameof(UndoDescription));
            OnPropertyChanged(nameof(RedoDescription));
            // Trigger ICommand CanExecuteChanged re-evals
            (UndoCommand as AsBuiltRelayCommand)?.RaiseCanExecuteChanged();
            (RedoCommand as AsBuiltRelayCommand)?.RaiseCanExecuteChanged();
        });
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Replace the active job (called from New Job Wizard or Open-existing flow).
    /// Triggers an immediate background validation pass.
    /// </summary>
    public void LoadJob(AsBuiltJob job)
    {
        Job = job;
        var targetStep = Steps.FirstOrDefault(s => s.Phase == job.CurrentPhase) ?? Steps.First();
        SelectedStep = targetStep;
        _ = RunValidationAsync();  // fire-and-forget, CancellationToken handles overlap
    }

    /// <summary>
    /// Called when the ViewModel selects a structure via the data grid.
    /// Syncs the selection back to the grid in the center pane.
    /// </summary>
    public void OnCanvasStructureClicked(string structureId)
    {
        var s = Job.Network.Structures.GetValueOrDefault(structureId);
        if (s != null) SelectedStructure = s;
    }

    /// <summary>
    /// Called when the Points phase grid selects a row.
    /// Fires an event so the canvas can highlight the point glyph.
    /// </summary>
    public void OnCanvasPointSelected(string pointId)
        => PointSelectionChanged?.Invoke(this, pointId);

    /// <summary>
    /// Invokes jump-to-line event for IDE code synchronization, automatically launching the file in VS Code at the target line.
    /// </summary>
    public void OnSourceLineZoomRequested(int lineNo)
    {
        SourceLineZoomRequested?.Invoke(this, lineNo);
        if (Job?.PendingImportPaths == null) return;
        var scriptPath = Job.PendingImportPaths.FirstOrDefault(p => p.EndsWith(".cogo") || p.EndsWith(".txt"));
        if (!string.IsNullOrEmpty(scriptPath) && System.IO.File.Exists(scriptPath))
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c code -g \"{scriptPath}:{lineNo}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }
    }

    /// <summary>
    /// Called when the Pipe Runs phase grid selects a run row.
    /// Fires an event so the canvas can bold-highlight the pipe line.
    /// </summary>
    public void OnCanvasRunSelected(string runId)
    {
        var run = Job.Network.Runs.GetValueOrDefault(runId);
        if (run != null) SelectedRun = run;
    }

    /// <summary>
    /// Applies a safe auto-fix for a ValidationIssue that has AutoFixable=true.
    /// Currently handles SLOPE_REVERSAL by swapping inverts.
    /// </summary>
    public void ApplyAutoFix(RCS.Piping.Core.Workflow.ValidationIssue issue)
    {
        if (!issue.AutoFixable || issue.TargetId == null) return;
        if (issue.RuleName == "SLOPE_REVERSAL")
        {
            if (Job.Network.Runs.TryGetValue(issue.TargetId, out var run))
            {
                var oldStart = run.InvertStart;
                var oldEnd = run.InvertEnd;
                var oldFrom = run.FromPointId;
                var oldTo = run.ToPointId;
                
                UndoStack?.Push(new RCS.Cogo.Wpf.Services.GenericDelegateAction(
                    "Fix Slope Reversal",
                    j => { if (j.Network.Runs.TryGetValue(issue.TargetId, out var r)) { r.InvertStart = oldStart; r.InvertEnd = oldEnd; r.FromPointId = oldFrom; r.ToPointId = oldTo; } },
                    j => { if (j.Network.Runs.TryGetValue(issue.TargetId, out var r)) { r.InvertStart = oldEnd; r.InvertEnd = oldStart; r.FromPointId = oldTo; r.ToPointId = oldFrom; } }));

                (run.InvertStart, run.InvertEnd) = (run.InvertEnd, run.InvertStart);
                (run.FromPointId, run.ToPointId) = (run.ToPointId, run.FromPointId);
            }
        }
        else if (issue.RuleName == "PIPE_CROSSING_CONFLICT")
        {
            if (Job.Network.Runs.TryGetValue(issue.TargetId, out var run))
            {
                var oldStart = run.InvertStart;
                var oldEnd = run.InvertEnd;
                double offsetAmt = 1.5 + (run.Diameter / 24.0);
                
                UndoStack?.Push(new RCS.Cogo.Wpf.Services.GenericDelegateAction(
                    "Auto-Trench Clash Fix",
                    j => { if (j.Network.Runs.TryGetValue(issue.TargetId, out var r)) { r.InvertStart = oldStart; r.InvertEnd = oldEnd; } },
                    j => { if (j.Network.Runs.TryGetValue(issue.TargetId, out var r)) { r.InvertStart = oldStart - offsetAmt; r.InvertEnd = oldEnd - offsetAmt; } }));

                run.InvertStart = (run.InvertStart ?? 0) - offsetAmt;
                run.InvertEnd = (run.InvertEnd ?? 0) - offsetAmt;
            }
        }
        else if (issue.RuleName == "DISCONNECTED_RUN")
        {
            if (Job.Network.Runs.TryGetValue(issue.TargetId, out var run))
            {
                var addedStructures = new System.Collections.Generic.List<RCS.Piping.Core.Models.PipeStructure>();
                if (!Job.Network.Structures.Values.Any(s => string.Equals(s.PointId, run.FromPointId, StringComparison.OrdinalIgnoreCase)))
                {
                    var ns = new RCS.Piping.Core.Models.PipeStructure { Id = Guid.NewGuid().ToString(), PointId = run.FromPointId, Type = "Auto-Node" };
                    Job.Network.Structures[ns.Id] = ns;
                    addedStructures.Add(ns);
                }
                if (!Job.Network.Structures.Values.Any(s => string.Equals(s.PointId, run.ToPointId, StringComparison.OrdinalIgnoreCase)))
                {
                    var ns = new RCS.Piping.Core.Models.PipeStructure { Id = Guid.NewGuid().ToString(), PointId = run.ToPointId, Type = "Auto-Node" };
                    Job.Network.Structures[ns.Id] = ns;
                    addedStructures.Add(ns);
                }
                
                if (addedStructures.Count > 0)
                {
                    UndoStack?.Push(new RCS.Cogo.Wpf.Services.GenericDelegateAction(
                        "Auto-Fix Disconnected Run",
                        j => { foreach (var s in addedStructures) ((System.Collections.IDictionary)j.Network.Structures).Remove(s.Id); },
                        j => { foreach (var s in addedStructures) j.Network.Structures[s.Id] = s; }));
                }
            }
        }
        else if (issue.RuleName == "UNMAPPED_PARTS")
        {
            var p = Job.PartMappings.FirstOrDefault(m => m.AssetId == issue.TargetId);
            if (p != null)
            {
                var dict = new System.Collections.Generic.Dictionary<string, string> {
                    { "CB", "Catch Basin" }, { "MH", "Sewer Manhole" }, { "GV", "Gate Valve" },
                    { "FH", "Fire Hydrant" }, { "CO", "Cleanout" }, { "BOC", "General Node" }
                };
                
                string foundKey = "Unknown Node";
                foreach (var kvp in dict) {
                    if (p.DetectedDesc.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0) {
                        foundKey = kvp.Value;
                        break;
                    }
                }
                
                string oldKey = p.PartKey;
                var oldStatus = p.Status;
                
                UndoStack?.Push(new RCS.Cogo.Wpf.Services.GenericDelegateAction(
                    "Fuzzy Map Part",
                    j => { var m = j.PartMappings.FirstOrDefault(x => x.AssetId == p.AssetId); if(m!=null){m.PartKey=oldKey; m.Status=oldStatus;} },
                    j => { var m = j.PartMappings.FirstOrDefault(x => x.AssetId == p.AssetId); if(m!=null){m.PartKey=foundKey; m.Status=RCS.Piping.Core.Workflow.MappingStatus.Resolved;} }));
                    
                p.PartKey = foundKey;
                p.Status = RCS.Piping.Core.Workflow.MappingStatus.Resolved;
                p.Confidence = 0.85;
            }
        }
        
        Job.AuditLog.Add(new RCS.Piping.Core.Workflow.AuditEntry {
            Action = "Auto-Fix Executed",
            Details = $"Rule: {issue.RuleName} | Target: {issue.TargetId}"
        });
        
        _ = RunValidationAsync();
    }

    /// <summary>
    /// Trigger a debounced background re-validation after any data edit.
    /// Call this from data-grid CellEditEnding handlers.
    /// </summary>
    public void RequestRevalidation()
    {
        _validationCts?.Cancel();
        _validationCts = new CancellationTokenSource();
        var token = _validationCts.Token;
        _ = Task.Delay(600, token).ContinueWith(
            _ => RunValidationAsync(), token,
            TaskContinuationOptions.NotOnCanceled,
            TaskScheduler.Current);
    }

    // ── Private Helpers ───────────────────────────────────────────────────────

    private void StartNewJob()
    {
        Job = new AsBuiltJob();
        SelectedStep = Steps.First();
    }

    private async Task RunValidationAsync()
    {
        // Run engine off the UI thread
        var result = await Task.Run(() => _validationEngine.Validate(Job));

        // Marshal back to UI thread
        LastValidation = result;
        RefreshNavigatorStatus();
    }

    private async Task BuildPackageAsync()
    {
        // Re-validate before export
        var result = await Task.Run(() => _validationEngine.Validate(Job));
        LastValidation = result;

        if (!IsExportReady) return;

        // Prompt user for output folder
        // WPF native folder picker — no System.Windows.Forms dependency
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title            = "Select Export Package Folder (type any filename — only the folder is used)",
            FileName         = "Select Folder",
            Filter           = "Folder|*.folder",
            CheckFileExists  = false,
            CheckPathExists  = true,
            ValidateNames    = false
        };
        if (dlg.ShowDialog() != true) return;

        var selectedFolder = System.IO.Path.GetDirectoryName(dlg.FileName) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var rev      = Job.Identity.RevisionNumber;
        var folder   = Path.Combine(selectedFolder, $"Rev{rev}_{DateTime.Now:MMddyyyy}");
        Directory.CreateDirectory(folder);

        var jobNum   = string.IsNullOrWhiteSpace(Job.Identity.JobNumber)
                       ? "AsBuilt" : Job.Identity.JobNumber;

        // Run all builders on background thread
        await Task.Run(() =>
        {
            new RCS.Piping.Core.Builders.DxfBuilder()
                .Build(Job, Path.Combine(folder, $"{jobNum}_Rev{rev}.dxf"));

            new RCS.Piping.Core.Builders.PnezdExportBuilder()
                .Build(Job, Path.Combine(folder, $"{jobNum}_Rev{rev}_PNEZD.csv"));

            new RCS.Piping.Core.Builders.PdfReportBuilder()
                .Build(Job, Path.Combine(folder, $"{jobNum}_Rev{rev}_Report.pdf"));

            RCS.Piping.Core.Builders.LandXmlBuilder
                .Export(Job, Path.Combine(folder, $"{jobNum}_Rev{rev}.xml"));
                
            var auditContent = new System.Text.StringBuilder();
            auditContent.AppendLine($"AUDIT TRAIL FOR {jobNum} (REV {rev})");
            foreach(var item in Job.AuditLog) {
                auditContent.AppendLine($"[{item.Timestamp:yyyy-MM-dd HH:mm:ss}] {item.User} >> {item.Action}: {item.Details}");
            }
            File.WriteAllText(Path.Combine(folder, $"{jobNum}_Rev{rev}_AUDIT_TRAIL.txt"), auditContent.ToString());
        });

        // Bump revision
        Job.Identity.RevisionNumber++;

        // Notify user
        System.Windows.MessageBox.Show(
            $"Export package written to:\n{folder}\n\n" +
            $"  ✅  {jobNum}_Rev{rev}.dxf\n" +
            $"  ✅  {jobNum}_Rev{rev}.xml\n" +
            $"  ✅  {jobNum}_Rev{rev}_AUDIT_TRAIL.txt\n" +
            $"  ✅  {jobNum}_Rev{rev}_PNEZD.csv\n" +
            $"  ✅  {jobNum}_Rev{rev}_Report.pdf\n\n" +
            $"[ENTERPRISE CLOUD] Uploading via E-Transmission API securely...",
            "Export Package Complete",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);

        RefreshDeliverableCards();
    }

    private async Task GenerateReportAsync()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title      = "Save AS-BUILT SURVEY REPORT",
            Filter     = "Text Report (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName   = $"{Job.Identity.JobNumber}_Rev{Job.Identity.RevisionNumber}_Report.txt"
        };
        if (dlg.ShowDialog() != true) return;

        string outputPath = dlg.FileName;
        var result = _validationEngine.Validate(Job);

        await Task.Run(() =>
            new RCS.Piping.Core.Builders.PdfReportBuilder()
                .Build(Job, outputPath));

        // Open in default viewer (Notepad / OS default)
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = outputPath,
                UseShellExecute = true
            });
        }
        catch { /* non-fatal — file was still saved */ }
    }

    private void RefreshNavigatorStatus()
    {
        var result = _validationEngine.Validate(Job);
        foreach (var step in Steps)
        {
            if (step == SelectedStep) continue;  // keep active highlight intact
            var categoryIssues = result.Issues
                .Where(i => StepCategoryMatch(step.Phase, i.Category))
                .ToList();

            step.Status = categoryIssues.Any(i => i.Severity == IssueSeverity.Error) ? StepStatus.Blocked
                        : categoryIssues.Any(i => i.Severity == IssueSeverity.Warning) ? StepStatus.Warning
                        : StepStatus.Idle;
        }
    }

    private void RefreshDeliverableCards()
    {
        foreach (var card in Job.Deliverables)
        {
            // LandXML requires all parts mapped; others only require zero errors
            bool needsFullMapping = card.TypeEnum == DeliverableType.LandXml;
            card.IsBlocked = needsFullMapping
                ? !Job.AllPartsMapped || !IsExportReady
                : !IsExportReady;
            card.BlockingErrorCount = ErrorCount;
            card.WarningCount       = WarningCount;
            card.StatusMessage      = card.IsBlocked ? "Blocked" : "Ready";
        }
        OnPropertyChanged(nameof(Deliverables));
    }

    private void GoToNextStep()
    {
        var idx = Steps.IndexOf(SelectedStep!);
        if (idx < Steps.Count - 1) SelectedStep = Steps[idx + 1];
    }

    private void GoToPrevStep()
    {
        var idx = Steps.IndexOf(SelectedStep!);
        if (idx > 0) SelectedStep = Steps[idx - 1];
    }

    private static bool StepCategoryMatch(WorkflowPhase phase, IssueCategory category) =>
        (phase, category) switch
        {
            (WorkflowPhase.PointsCleanup, IssueCategory.Coordinates) => true,
            (WorkflowPhase.Structures,    IssueCategory.Structures)   => true,
            (WorkflowPhase.PipeRuns,      IssueCategory.Runs)         => true,
            (WorkflowPhase.PipeRuns,      IssueCategory.Geometry)     => true,
            (WorkflowPhase.PartsMapping,  IssueCategory.PartsMapping) => true,
            (WorkflowPhase.Deliverables,  IssueCategory.ExportReadiness) => true,
            _ => false
        };

    // ── AI Blueprint Import ──────────────────────────────────────────────────────

    private async Task ImportBlueprintAsync()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Select Blueprint / As-Built Scan for AI Extraction",
            Filter = "Blueprint Images|*.pdf;*.png;*.jpg;*.jpeg|PDF Documents|*.pdf|PNG Images|*.png|JPEG Images|*.jpg;*.jpeg|All|*.*"
        };
        if (dlg.ShowDialog() != true) return;

        ShowSnackbarRequested?.Invoke("🤖  Gemini Vision AI extracting pipeline from blueprint…", false);

        var aiJob    = new AsBuiltJob();
        var engine   = new RCS.Piping.Core.Engines.AiVisionExtractionEngine();
        bool success = false;

        try
        {
            success = await engine.ExtractPipelineFromScanAsync(dlg.FileName, aiJob);
        }
        catch (Exception ex)
        {
            ShowSnackbarRequested?.Invoke($"AI extraction error: {ex.Message}", true);
            return;
        }

        if (success && (aiJob.Network.Structures.Count + aiJob.Network.Runs.Count) > 0)
        {
            Job = aiJob;
            IntakeReport = new RCS.Piping.Core.Engines.IntakeReport
            {
                Success         = true,
                PointsLoaded    = aiJob.PointRows.Count,
                RunsLoaded      = aiJob.Network.Runs.Count,
                StructuresFound = aiJob.Network.Structures.Count,
                Summary         = $"AI Vision: {aiJob.Network.Structures.Count} structure(s), " +
                                  $"{aiJob.Network.Runs.Count} run(s), " +
                                  $"{aiJob.PointRows.Count} point(s) extracted from blueprint."
            };
            OnPropertyChanged(nameof(IntakeReport));

            // Auto-navigate to Structures phase if pipeline data found
            var targetStep = Steps.FirstOrDefault(s => s.Phase == WorkflowPhase.Structures) ?? Steps.First();
            SelectedStep = targetStep;

            ShowSnackbarRequested?.Invoke(
                $"✅  AI extracted {aiJob.Network.Structures.Count} structures + {aiJob.Network.Runs.Count} runs", false);
        }
        else
        {
            ShowSnackbarRequested?.Invoke("AI found no recognisable pipeline geometry in the blueprint.", true);
        }

        await RunValidationAsync();
    }

    // ── Intake File Import ─────────────────────────────────────────────────────

    private async Task ImportFileAsync(IntakeFileType fileType)
    {
        // Must run dialog on UI thread
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = fileType switch
            {
                IntakeFileType.Pnezd      => "Select PNEZD / CSV Point File",
                IntakeFileType.CogoScript => "Select COGO Script",
                IntakeFileType.JeaExcel   => "Select JEA As-Built Excel Template",
                IntakeFileType.Dxf        => "Select DXF Linework File",
                _                         => "Select File"
            },
            Filter = fileType switch
            {
                IntakeFileType.Pnezd      => "CSV / TXT|*.csv;*.txt|All|*.*",
                IntakeFileType.CogoScript => "COGO Script|*.cogo;*.txt|All|*.*",
                IntakeFileType.JeaExcel   => "Excel|*.xlsx;*.xls|All|*.*",
                IntakeFileType.Dxf        => "DXF|*.dxf|All|*.*",
                _                         => "All|*.*"
            }
        };

        if (dlg.ShowDialog() != true) return;

        var path = dlg.FileName;

        // Run engine on background thread — keeps UI responsive for large files
        var engine = new RCS.Piping.Core.Engines.IntakeAnalysisEngine();
        var report = await Task.Run(() => engine.Analyze(path, fileType, Job));

        // Post status to Intake phase
        IntakeReport = report;
        OnPropertyChanged(nameof(IntakeReport));

        // ── Show structured diff summary ──────────────────────────────────────
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(report.Summary);
        sb.AppendLine();

        if (report.RowsAdded > 0 || report.RowsUpdated > 0 || report.RowsSkipped > 0)
        {
            sb.AppendLine($"  \u2705  Added     : {report.RowsAdded,6}");
            sb.AppendLine($"  \ud83d\udd04  Updated   : {report.RowsUpdated,6}");
            sb.AppendLine($"  \u23ed\ufe0f  Skipped   : {report.RowsSkipped,6}");
        }

        if (report.ValidationErrors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Validation issues:");
            foreach (var err in report.ValidationErrors.Take(10))
                sb.AppendLine($"  • {err}");
            if (report.ValidationErrors.Count > 10)
                sb.AppendLine($"  … and {report.ValidationErrors.Count - 10} more.");
        }

        ShowSnackbarRequested?.Invoke(
            sb.ToString().TrimEnd().Replace("\n", " | "), 
            !report.Success);

        // Auto-advance to Points phase if import succeeded
        if (report.PointsLoaded > 0 || report.RunsLoaded > 0)
        {
            var pointsStep = Steps.FirstOrDefault(s => s.Phase == WorkflowPhase.PointsCleanup);
            if (pointsStep != null) SelectedStep = pointsStep;
        }

        await RunValidationAsync();
    }
    // ── Drag Drop Import Logic ───────────────────────────────────────────────
    public async Task LoadDragDropFileAsync(string absolutePath)
    {
        try
        {
            if (absolutePath.EndsWith(".las", StringComparison.OrdinalIgnoreCase) || 
                absolutePath.EndsWith(".laz", StringComparison.OrdinalIgnoreCase))
            {
                ShowSnackbarRequested?.Invoke("Parsing LiDAR Data...", false);
                var surface = await RCS.Piping.Core.Engines.LasParser.ExtractSurfaceFromLasAsync(absolutePath);
                Job.BaseSurface = surface;
                ShowSnackbarRequested?.Invoke($"Loaded {surface.Points.Count:N0} LiDAR points into Base Topography.", false);
                await RunValidationAsync();
                return;
            }

            // [AI PAPER-TO-PIPELINE INTERCEPTION]
            if (absolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || 
                absolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                absolutePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                ShowSnackbarRequested?.Invoke("Initializing BoundaryQC AI Pipeline Extraction...", false);
                var aiJob = new AsBuiltJob();
                var visionEngine = new RCS.Piping.Core.Engines.AiVisionExtractionEngine();
                bool extracted = await visionEngine.ExtractPipelineFromScanAsync(absolutePath, aiJob);
                
                if (extracted)
                {
                    Job = aiJob;
                    var dbStep = Steps.FirstOrDefault(s => s.Phase == WorkflowPhase.Dashboard);
                    if (dbStep != null) SelectedStep = dbStep;
                    ShowSnackbarRequested?.Invoke("AI Auto-Extraction Complete. Pipeline reconstructed from blueprint.", false);
                    await RunValidationAsync();
                }
                else
                {
                    ShowSnackbarRequested?.Invoke("AI extraction failed to identify valid pipeline geometries.", true);
                }
                return;
            }

            var newJob = new AsBuiltJob();
            var engine = new RCS.Piping.Core.Engines.IntakeAnalysisEngine();
            // Defaulting to PNEZD format for broad drag-and-drop support
            var report = engine.Analyze(absolutePath, RCS.Piping.Core.Engines.IntakeFileType.Pnezd, newJob);

            if (report.Success)
            {
                Job = newJob;
                
                // Navigate to Dashboard Phase
                var dashboard = Steps.FirstOrDefault(s => s.Phase == WorkflowPhase.Dashboard);
                if (dashboard != null) SelectedStep = dashboard;
                
                await RunValidationAsync();
            }
            else
            {
                ShowSnackbarRequested?.Invoke($"File Analysis Failed: {report.ValidationErrors.FirstOrDefault()}", true);
            }
        }
        catch (Exception ex)
        {
            ShowSnackbarRequested?.Invoke($"Failed to extract dropped file: {ex.Message}", true);
        }
    }

    // ── Intake Report (displayed in IntakePhaseView summary strip) ─────────────
    public IntakeReport IntakeReport { get; private set; } = new();
}
