using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using RCS.Cogo.Wpf.Commands;
using RCS.Piping.Core.Workflow;

namespace RCS.Cogo.Wpf.ViewModels;

// ── Recent job summary shown on the dashboard ─────────────────────────────────

public class RecentJobItem : ViewModelBase
{
    public string   JobNumber      { get; set; } = string.Empty;
    public string   ClientName     { get; set; } = string.Empty;
    public int      RevisionNumber { get; set; } = 1;
    public DateTime LastModified   { get; set; } = DateTime.Now;
    public string   FilePath       { get; set; } = string.Empty;
    public int      PendingIssues  { get; set; }

    // Derived display strings
    public string DisplayName   => JobNumber.Length > 0 ? JobNumber : ClientName;
    public string RevLabel      => $"Rev {RevisionNumber}";
    public string ModifiedLabel => LastModified.ToString("MM/dd/yyyy");
    public string IssueLabel    => PendingIssues > 0 ? $"⚠ {PendingIssues} issues" : "✅ Clean";
    public bool   HasIssues     => PendingIssues > 0;

    public ICommand? OpenCommand { get; set; }
}

// ── Job Dashboard ViewModel ───────────────────────────────────────────────────

public class JobDashboardViewModel : ViewModelBase
{
    private const string RecentJobsFileName = "recent_jobs.json";
    private readonly string _recentJobsPath;

    // Commands wired by the parent ShellViewModel
    public ICommand? NewJobCommand    { get; set; }
    public ICommand? OpenJobCommand   { get; set; }
    public ICommand? ImportDataCommand { get; set; }
    public ICommand? TemplatesCommand  { get; set; }

    public ObservableCollection<RecentJobItem> RecentJobs { get; } = [];

    private string _pendingSummary = string.Empty;
    public string PendingSummary
    {
        get => _pendingSummary;
        set { _pendingSummary = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasPendingIssues)); }
    }
    public bool HasPendingIssues => !string.IsNullOrEmpty(PendingSummary);

    public string AppVersion => $"RCS Cogo Enterprise v{GetType().Assembly.GetName().Version?.ToString(3) ?? "3.0"}";

    public JobDashboardViewModel()
    {
        _recentJobsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RCS.Cogo", RecentJobsFileName);

        LoadRecentJobs();
        RefreshPendingSummary();
    }

    public void LoadRecentJobs()
    {
        RecentJobs.Clear();

        if (!File.Exists(_recentJobsPath))
            return;

        try
        {
            var json  = File.ReadAllText(_recentJobsPath);
            var items = JsonSerializer.Deserialize<List<RecentJobRecord>>(json) ?? [];

            foreach (var rec in items.Where(r => File.Exists(r.Path)).Take(10))
            {
                var item = new RecentJobItem
                {
                    JobNumber      = rec.JobNumber,
                    ClientName     = rec.ClientName,
                    RevisionNumber = rec.RevisionNumber,
                    LastModified   = rec.LastModified,
                    FilePath       = rec.Path
                };
                item.OpenCommand = new RelayCommand(_ => OpenJobCommand?.Execute(item.FilePath));
                RecentJobs.Add(item);
            }
        }
        catch { /* graceful — corrupt recent list is non-fatal */ }
    }

    /// <summary>Persist a newly opened/saved job into the recent list.</summary>
    public void PushRecentJob(AsBuiltJob job, string filePath)
    {
        var dir = Path.GetDirectoryName(_recentJobsPath)!;
        Directory.CreateDirectory(dir);

        var existing = RecentJobs.FirstOrDefault(r => r.FilePath == filePath);
        if (existing != null)
            RecentJobs.Remove(existing);

        var record = new RecentJobRecord
        {
            JobNumber      = job.Identity.JobNumber,
            ClientName     = job.Identity.ClientName,
            RevisionNumber = job.Identity.RevisionNumber,
            LastModified   = DateTime.Now,
            Path           = filePath
        };

        var records = RecentJobs
            .Select(r => new RecentJobRecord
            {
                JobNumber      = r.JobNumber,
                ClientName     = r.ClientName,
                RevisionNumber = r.RevisionNumber,
                LastModified   = r.LastModified,
                Path           = r.FilePath
            })
            .Prepend(record)
            .Take(10)
            .ToList();

        File.WriteAllText(_recentJobsPath, JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true }));
        LoadRecentJobs();
    }

    private void RefreshPendingSummary()
    {
        var count = RecentJobs.Sum(j => j.PendingIssues);
        PendingSummary = count > 0 ? $"{count} job(s) have pending validation issues" : string.Empty;
    }

    private sealed record RecentJobRecord
    {
        public string   JobNumber      { get; init; } = string.Empty;
        public string   ClientName     { get; init; } = string.Empty;
        public int      RevisionNumber { get; init; } = 1;
        public DateTime LastModified   { get; init; }
        public string   Path           { get; init; } = string.Empty;
    }
}
