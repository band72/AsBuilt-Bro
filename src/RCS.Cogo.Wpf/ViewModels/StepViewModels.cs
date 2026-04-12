using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using RCS.Cogo.Wpf.Commands;
using RCS.Piping.Core.Models;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.ViewModels;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// PointRow ViewModel wrapper (adds duplicate flag)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class PointRowVm : ViewModelBase
{
    private bool _isDuplicate;
    public string PointId     { get; set; } = string.Empty;
    public double Northing    { get; set; }
    public double Easting     { get; set; }
    public double Elevation   { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool   IsDuplicate
    {
        get => _isDuplicate;
        set { _isDuplicate = value; OnPropertyChanged(); }
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// StepPointsViewModel
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class StepPointsViewModel : ViewModelBase
{
    private string        _pointsFilter = string.Empty;
    private PointRowVm?   _selectedPoint;

    public ObservableCollection<PointRowVm> Points { get; } = [];
    public ICollectionView PointsView { get; }

    public string PointsFilter
    {
        get => _pointsFilter;
        set { _pointsFilter = value ?? string.Empty; OnPropertyChanged(); PointsView.Refresh(); }
    }

    public PointRowVm? SelectedPoint
    {
        get => _selectedPoint;
        set { _selectedPoint = value; OnPropertyChanged(); }
    }

    public int TotalCount     => Points.Count;
    public int DuplicateCount => Points.Count(p => p.IsDuplicate);

    public ICommand ImportPointsCommand         { get; }
    public ICommand FindDuplicatesCommand       { get; }
    public ICommand AutoFixDescriptionsCommand  { get; }
    public ICommand ClearFilterCommand          { get; }

    public StepPointsViewModel()
    {
        PointsView = CollectionViewSource.GetDefaultView(Points);
        PointsView.Filter = o => o is PointRowVm pt
            && (string.IsNullOrWhiteSpace(_pointsFilter)
                || pt.PointId.Contains(_pointsFilter, StringComparison.OrdinalIgnoreCase)
                || pt.Description.Contains(_pointsFilter, StringComparison.OrdinalIgnoreCase));

        ImportPointsCommand        = new RelayCommand(_ => ImportPoints());
        FindDuplicatesCommand      = new RelayCommand(_ => FindDuplicates());
        AutoFixDescriptionsCommand = new RelayCommand(_ => AutoFixDescriptions());
        ClearFilterCommand         = new RelayCommand(_ => PointsFilter = string.Empty);
    }

    public void LoadFromJob(AsBuiltJob job)
    {
        Points.Clear();
        foreach (var r in job.PointRows)
            Points.Add(new PointRowVm
            {
                PointId     = r.PointId,
                Northing    = r.Northing,
                Easting     = r.Easting,
                Elevation   = r.Elevation,
                Description = r.Description
            });
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(DuplicateCount));
    }

    private void ImportPoints() { /* Wired from ShellWindow via event/command */ }

    private void FindDuplicates()
    {
        // Proximity threshold: 0.01 ft
        const double thr = 0.01;
        var groups = Points.GroupBy(p => (Math.Round(p.Northing / thr), Math.Round(p.Easting / thr)));
        foreach (var g in groups)
        foreach (var p in g)
            p.IsDuplicate = g.Count() > 1;
        OnPropertyChanged(nameof(DuplicateCount));
    }

    private void AutoFixDescriptions()
    {
        // Normalise to UPPER + trim — full code-library lookup wired later
        foreach (var p in Points)
            p.Description = p.Description.Trim().ToUpper();
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// StructureVm wrapper (adds HasError / SumpDepth / HasSump)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class StructureVm : ViewModelBase
{
    public string  Id          { get; set; } = string.Empty;
    public string  PointId     { get; set; } = string.Empty;
    public string  Type        { get; set; } = "MH";
    public double? RimElevation { get; set; }
    public double? InvertIn    { get; set; }
    public double? InvertOut   { get; set; }

    public double? SumpDepth => (InvertIn.HasValue && InvertOut.HasValue)
        ? Math.Abs(InvertIn.Value - InvertOut.Value) : null;
    public bool HasSump => SumpDepth > 0.05;
    public bool HasError => !RimElevation.HasValue;

    public static StructureVm FromDomain(PipeStructure s) => new()
    {
        Id = s.Id, PointId = s.PointId, Type = s.Type,
        RimElevation = s.RimElevation, InvertIn = s.InvertIn, InvertOut = s.InvertOut
    };
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// StepStructuresViewModel
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class StepStructuresViewModel : ViewModelBase
{
    private StructureVm? _selected;

    public ObservableCollection<StructureVm> Structures { get; } = [];
    public StructureVm? SelectedStructure
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); }
    }

    public ICommand AddStructureCommand     { get; }
    public ICommand RemoveStructureCommand  { get; }
    public ICommand InferElevationsCommand  { get; }

    public StepStructuresViewModel()
    {
        AddStructureCommand     = new RelayCommand(_ => Structures.Add(new StructureVm { Id = Guid.NewGuid().ToString() }));
        RemoveStructureCommand  = new RelayCommand(_ => { if (SelectedStructure != null) Structures.Remove(SelectedStructure); });
        InferElevationsCommand  = new RelayCommand(_ => InferElevations());
    }

    public void LoadFromJob(AsBuiltJob job)
    {
        Structures.Clear();
        foreach (var s in job.Network.GetAllStructures())
            Structures.Add(StructureVm.FromDomain(s));
    }

    private void InferElevations()
    {
        // Best-effort: if rim is missing, set to max invert + 4.0 ft
        foreach (var s in Structures.Where(x => !x.RimElevation.HasValue))
        {
            var maxInv = new[] { s.InvertIn, s.InvertOut }.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty(0).Max();
            if (maxInv > 0) s.RimElevation = maxInv + 4.0;
        }
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// PipeRunVm wrapper (adds HasSlopeReversal / FlowArrow)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class PipeRunVm : ViewModelBase
{
    public string  Id            { get; set; } = string.Empty;
    public string  FromPointId   { get; set; } = string.Empty;
    public string  ToPointId     { get; set; } = string.Empty;
    public double  Diameter      { get; set; }
    public string  Material      { get; set; } = string.Empty;
    public double  ComputedLength { get; set; }
    public double  SlopePercent  { get; set; }
    public double? InvertStart   { get; set; }
    public double? InvertEnd     { get; set; }
    public bool    FlowReversed  { get; set; }

    public bool   HasSlopeReversal => SlopePercent < 0;
    public string FlowArrow        => FlowReversed ? "←" : "→";

    public static PipeRunVm FromDomain(PipeRun r) => new()
    {
        Id = r.Id, FromPointId = r.FromPointId, ToPointId = r.ToPointId,
        Diameter = r.Diameter, Material = r.Material,
        ComputedLength = r.ComputedLength, SlopePercent = r.SlopePercent,
        InvertStart = r.InvertStart, InvertEnd = r.InvertEnd, FlowReversed = r.FlowReversed
    };
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// StepPipeRunsViewModel
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class StepPipeRunsViewModel : ViewModelBase
{
    private PipeRunVm? _selected;

    public ObservableCollection<PipeRunVm> Runs { get; } = [];
    public PipeRunVm? SelectedRun
    {
        get => _selected;
        set { _selected = value; OnPropertyChanged(); }
    }

    public int    RunCount       => Runs.Count;
    public double TotalLength    => Runs.Sum(r => r.ComputedLength);
    public int    ReversalCount  => Runs.Count(r => r.HasSlopeReversal);

    public ICommand AddRunCommand          { get; }
    public ICommand RemoveRunCommand       { get; }
    public ICommand ComputeSlopesCommand   { get; }
    public ICommand ReverseFlowCommand     { get; }
    public ICommand AutoChainCommand       { get; }

    public StepPipeRunsViewModel()
    {
        AddRunCommand        = new RelayCommand(_ => { Runs.Add(new PipeRunVm { Id = Guid.NewGuid().ToString() }); RefreshTotals(); });
        RemoveRunCommand     = new RelayCommand(_ => { if (SelectedRun != null) { Runs.Remove(SelectedRun); RefreshTotals(); } });
        ComputeSlopesCommand = new RelayCommand(_ => ComputeSlopes());
        ReverseFlowCommand   = new RelayCommand(_ => { if (SelectedRun != null) SelectedRun.FlowReversed = !SelectedRun.FlowReversed; });
        AutoChainCommand     = new RelayCommand(_ => { /* Chain by shared structure points */ });
    }

    public void LoadFromJob(AsBuiltJob job)
    {
        Runs.Clear();
        foreach (var r in job.Network.GetAllRuns())
            Runs.Add(PipeRunVm.FromDomain(r));
        RefreshTotals();
    }

    private void ComputeSlopes()
    {
        foreach (var r in Runs.Where(x => x.InvertStart.HasValue && x.InvertEnd.HasValue && x.ComputedLength > 0))
            r.SlopePercent = (r.InvertStart!.Value - r.InvertEnd!.Value) / r.ComputedLength * 100.0;
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(RunCount));
        OnPropertyChanged(nameof(TotalLength));
        OnPropertyChanged(nameof(ReversalCount));
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// StepPartsMappingViewModel
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class PartMappingVm : ViewModelBase
{
    public string        AssetId        { get; set; } = string.Empty;
    public string        DisplayName    { get; set; } = string.Empty;
    public string        DetectedDesc   { get; set; } = string.Empty;
    public string?       ProposedPartKey { get; set; }
    public string        PartKey        { get; set; } = string.Empty;
    public string        Manufacturer   { get; set; } = string.Empty;
    public double        Confidence     { get; set; }
    public MappingStatus Status         { get; set; } = MappingStatus.Pending;
    public double        ConfidencePct  => Confidence * 100.0;
}

public class StepPartsMappingViewModel : ViewModelBase
{
    private PartMappingVm? _selectedMapping;
    private string         _selectedFilter = "All";

    public ObservableCollection<PartMappingVm> Mappings { get; } = [];
    public ICollectionView MappingsView { get; }

    public string[] FilterOptions { get; } = ["All", "Pending", "Resolved", "Skipped", "Error"];
    public string SelectedFilter
    {
        get => _selectedFilter;
        set { _selectedFilter = value; OnPropertyChanged(); MappingsView.Refresh(); }
    }

    public PartMappingVm? SelectedMapping
    {
        get => _selectedMapping;
        set { _selectedMapping = value; OnPropertyChanged(); }
    }

    public int ResolvedCount => Mappings.Count(m => m.Status == MappingStatus.Resolved);
    public int PendingCount  => Mappings.Count(m => m.Status == MappingStatus.Pending);
    public int SkippedCount  => Mappings.Count(m => m.Status == MappingStatus.Skipped);

    public ICommand AutoMapCommand          { get; }
    public ICommand AcceptAllProposedCommand { get; }
    public ICommand SkipUnmappedCommand     { get; }

    public StepPartsMappingViewModel()
    {
        MappingsView = CollectionViewSource.GetDefaultView(Mappings);
        MappingsView.Filter = o => o is PartMappingVm m
            && (_selectedFilter == "All" || m.Status.ToString() == _selectedFilter);

        AutoMapCommand          = new RelayCommand(_ => AutoMap());
        AcceptAllProposedCommand = new RelayCommand(_ => AcceptAll());
        SkipUnmappedCommand     = new RelayCommand(_ => SkipUnmapped());
    }

    public void LoadFromJob(AsBuiltJob job)
    {
        Mappings.Clear();
        foreach (var m in job.PartMappings)
            Mappings.Add(new PartMappingVm
            {
                AssetId = m.AssetId, DisplayName = m.DisplayName,
                DetectedDesc = m.DetectedDesc, ProposedPartKey = m.ProposedPartKey,
                PartKey = m.PartKey, Manufacturer = m.Manufacturer,
                Confidence = m.Confidence, Status = m.Status
            });
        RefreshCounts();
    }

    private void AutoMap()
    {
        // Accept high-confidence proposals (>= 0.80)
        foreach (var m in Mappings.Where(x => x.Status == MappingStatus.Pending && x.ProposedPartKey != null && x.Confidence >= 0.80))
        {
            m.PartKey = m.ProposedPartKey!;
            m.Status  = MappingStatus.Resolved;
        }
        RefreshCounts();
    }

    private void AcceptAll()
    {
        foreach (var m in Mappings.Where(x => x.ProposedPartKey != null))
        {
            m.PartKey = m.ProposedPartKey!;
            m.Status  = MappingStatus.Resolved;
        }
        RefreshCounts();
    }

    private void SkipUnmapped()
    {
        foreach (var m in Mappings.Where(x => x.Status == MappingStatus.Pending))
            m.Status = MappingStatus.Skipped;
        RefreshCounts();
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(ResolvedCount));
        OnPropertyChanged(nameof(PendingCount));
        OnPropertyChanged(nameof(SkippedCount));
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// ValidationIssueVm wrapper
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class ValidationIssueVm : ViewModelBase
{
    public IssueSeverity  Severity     { get; set; }
    public IssueCategory  Category     { get; set; }
    public string         Message      { get; set; } = string.Empty;
    public string?        TargetId     { get; set; }
    public string?        RuleName     { get; set; }
    public string?        SuggestedFix { get; set; }
    public bool           AutoFixable  { get; set; }

    public string SeverityGlyph => Severity switch
    {
        IssueSeverity.Error   => "🔴",
        IssueSeverity.Warning => "⚠",
        _                     => "ℹ"
    };
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// StepValidationViewModel
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class StepValidationViewModel : ViewModelBase
{
    private readonly ValidationEngine _engine = new();
    private ValidationIssueVm?        _selectedIssue;
    private AsBuiltJob?               _job;

    public ObservableCollection<ValidationIssueVm> Issues { get; } = [];
    public ICollectionView IssuesView { get; }

    public ValidationIssueVm? SelectedIssue
    {
        get => _selectedIssue;
        set { _selectedIssue = value; OnPropertyChanged(); }
    }

    public int  ErrorCount       => Issues.Count(i => i.Severity == IssueSeverity.Error);
    public int  WarningCount     => Issues.Count(i => i.Severity == IssueSeverity.Warning);
    public int  PassCount        { get; private set; }
    public bool IsExportBlocked  => ErrorCount > 0;
    public string ExportGateLabel => IsExportBlocked
        ? $"🔴  Export Blocked — {ErrorCount} error(s)"
        : "✅  Export Ready";

    public ICommand RunValidationCommand { get; }
    public ICommand AutoFixAllCommand    { get; }
    public ICommand CopyReportCommand    { get; }

    public StepValidationViewModel()
    {
        IssuesView = CollectionViewSource.GetDefaultView(Issues);
        IssuesView.SortDescriptions.Add(new SortDescription(nameof(ValidationIssueVm.Severity), ListSortDirection.Ascending));

        RunValidationCommand = new RelayCommand(_ => RunValidation());
        AutoFixAllCommand    = new RelayCommand(_ => AutoFixAll());
        CopyReportCommand    = new RelayCommand(_ => CopyReport());
    }

    public void LoadFromJob(AsBuiltJob job)
    {
        _job = job;
        RunValidation();
    }

    private void RunValidation()
    {
        if (_job == null) return;
        Issues.Clear();
        var result = _engine.Validate(_job);
        foreach (var issue in result.Issues)
            Issues.Add(new ValidationIssueVm
            {
                Severity = issue.Severity, Category = issue.Category,
                Message = issue.Message, TargetId = issue.TargetId,
                RuleName = issue.RuleName, SuggestedFix = issue.SuggestedFix,
                AutoFixable = issue.AutoFixable
            });
        PassCount = result.Issues.Count == 0 ? 1 : 0;
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(PassCount));
        OnPropertyChanged(nameof(IsExportBlocked));
        OnPropertyChanged(nameof(ExportGateLabel));
    }

    private void AutoFixAll() { /* Execute all AutoFixable fixes */ }

    private void CopyReport()
    {
        var lines = Issues.Select(i => $"[{i.Severity}] {i.RuleName} — {i.Message}");
        System.Windows.Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// StepDeliverablesViewModel
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class DeliverableCardVm : ViewModelBase
{
    private bool   _isEnabled = true;
    private string _statusMessage = string.Empty;

    public string          Type               { get; set; } = string.Empty;
    public DeliverableType TypeEnum           { get; set; }
    public bool            IsBlocked          { get; set; }
    public int             BlockingErrorCount { get; set; }
    public int             WarningCount       { get; set; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set { _isEnabled = value; OnPropertyChanged(); }
    }
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatusMessage)); }
    }
    public bool HasStatusMessage => !string.IsNullOrEmpty(_statusMessage);
}

public class StepDeliverablesViewModel : ViewModelBase
{
    private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    public string OutputFolder
    {
        get => _outputFolder;
        set { _outputFolder = value; OnPropertyChanged(); }
    }

    public bool CanBuildPackage { get; private set; } = true;

    public ObservableCollection<DeliverableCardVm> Deliverables   { get; } = [];
    public ObservableCollection<ExportRecord>      ExportHistory  { get; } = [];

    public ICommand BuildPackageCommand      { get; }
    public ICommand BrowseOutputFolderCommand { get; }

    private AsBuiltJob? _job;

    public StepDeliverablesViewModel()
    {
        BuildPackageCommand       = new RelayCommand(_ => BuildPackage(), _ => CanBuildPackage);
        BrowseOutputFolderCommand = new RelayCommand(_ => BrowseFolder());
    }

    public void LoadFromJob(AsBuiltJob job)
    {
        _job = job;
        Deliverables.Clear();
        foreach (var d in job.Deliverables)
            Deliverables.Add(new DeliverableCardVm
            {
                Type = d.Type, TypeEnum = d.TypeEnum,
                IsEnabled = d.IsEnabled, IsBlocked = d.IsBlocked,
                BlockingErrorCount = d.BlockingErrorCount,
                WarningCount = d.WarningCount,
                StatusMessage = d.StatusMessage
            });
        ExportHistory.Clear();
        foreach (var h in job.ExportHistory)
            ExportHistory.Add(h);
    }

    private void BuildPackage()
    {
        if (_job == null) return;
        try
        {
            var builder = new RCS.Piping.Core.Delivery.PackageBuilder(OutputFolder);
            var dir = builder.Build(_job);
            System.Diagnostics.Process.Start("explorer.exe", $"\"{dir}\"");
            // Sync status messages back
            LoadFromJob(_job);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Package build failed:\n{ex.Message}", "Build Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BrowseFolder()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Select export output folder",
            FileName = "Select Folder",
            InitialDirectory = System.IO.Directory.Exists(OutputFolder) ? OutputFolder : System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
        };
        if (dlg.ShowDialog() == true)
            OutputFolder = System.IO.Path.GetDirectoryName(dlg.FileName) ?? OutputFolder;
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// IntakeSummaryViewModel
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class IntakeSummaryViewModel : ViewModelBase
{
    public ObservableCollection<string> SourceFiles   { get; } = [];
    public ObservableCollection<string> IntakeIssues  { get; } = [];

    public int  PointCount     { get; private set; }
    public int  StructureCount { get; private set; }
    public int  RunCount       { get; private set; }
    public bool HasIntakeIssues => IntakeIssues.Count > 0;

    public ICommand ReImportCommand         { get; set; } = new RelayCommand(_ => { });
    public ICommand ProceedToPointsCommand  { get; set; } = new RelayCommand(_ => { });

    public void LoadFromJob(AsBuiltJob job)
    {
        SourceFiles.Clear();
        foreach (var f in job.PendingImportPaths)
            SourceFiles.Add(System.IO.Path.GetFileName(f));

        PointCount     = job.PointRows.Count;
        StructureCount = job.Network.Structures.Count;
        RunCount       = job.Network.Runs.Count;

        OnPropertyChanged(nameof(PointCount));
        OnPropertyChanged(nameof(StructureCount));
        OnPropertyChanged(nameof(RunCount));
        OnPropertyChanged(nameof(HasIntakeIssues));
    }
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// StepPreviewViewModel
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

public class StepPreviewViewModel : ViewModelBase
{
    private bool _showPoints     = true;
    private bool _showStructures;
    private bool _showRuns;
    private bool _showParts;

    public ObservableCollection<PointRowVm>   PointRows     { get; } = [];
    public ObservableCollection<StructureVm>  Structures    { get; } = [];
    public ObservableCollection<PipeRunVm>    Runs          { get; } = [];
    public ObservableCollection<PartMappingVm> PartMappings { get; } = [];

    public bool ShowPoints
    {
        get => _showPoints;
        set { _showPoints = value; OnPropertyChanged(); if (value) ClearOthers(nameof(ShowPoints)); }
    }
    public bool ShowStructures
    {
        get => _showStructures;
        set { _showStructures = value; OnPropertyChanged(); if (value) ClearOthers(nameof(ShowStructures)); }
    }
    public bool ShowRuns
    {
        get => _showRuns;
        set { _showRuns = value; OnPropertyChanged(); if (value) ClearOthers(nameof(ShowRuns)); }
    }
    public bool ShowParts
    {
        get => _showParts;
        set { _showParts = value; OnPropertyChanged(); if (value) ClearOthers(nameof(ShowParts)); }
    }

    public void LoadFromJob(AsBuiltJob job)
    {
        PointRows.Clear();
        foreach (var r in job.PointRows)
            PointRows.Add(new PointRowVm { PointId = r.PointId, Northing = r.Northing, Easting = r.Easting, Elevation = r.Elevation, Description = r.Description });

        Structures.Clear();
        foreach (var s in job.Network.GetAllStructures())
            Structures.Add(StructureVm.FromDomain(s));

        Runs.Clear();
        foreach (var r in job.Network.GetAllRuns())
            Runs.Add(PipeRunVm.FromDomain(r));

        PartMappings.Clear();
        foreach (var m in job.PartMappings)
            PartMappings.Add(new PartMappingVm { AssetId = m.AssetId, DisplayName = m.DisplayName, PartKey = m.PartKey, Manufacturer = m.Manufacturer, Status = m.Status });
    }

    private void ClearOthers(string active)
    {
        if (active != nameof(ShowPoints))     { _showPoints     = false; OnPropertyChanged(nameof(ShowPoints)); }
        if (active != nameof(ShowStructures)) { _showStructures = false; OnPropertyChanged(nameof(ShowStructures)); }
        if (active != nameof(ShowRuns))       { _showRuns       = false; OnPropertyChanged(nameof(ShowRuns)); }
        if (active != nameof(ShowParts))      { _showParts      = false; OnPropertyChanged(nameof(ShowParts)); }
    }
}
