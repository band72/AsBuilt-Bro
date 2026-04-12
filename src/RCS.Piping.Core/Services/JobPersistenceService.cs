using System.IO;
using System.Text.Json;
using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Services;

/// <summary>
/// Autosave / crash-restore service.
/// Serializes AsBuiltJob to %AppData%\RCS.Cogo\Jobs\{JobId}.rcsj on every save trigger.
/// </summary>
public class JobPersistenceService
{
    private static readonly string JobsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RCS.Cogo", "Jobs");

    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented      = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Save job to the standard jobs directory. Returns the file path.</summary>
    public string Save(AsBuiltJob job, string? explicitPath = null)
    {
        Directory.CreateDirectory(JobsDir);
        var path = explicitPath ?? DefaultPath(job);
        job.LastSaved = DateTime.UtcNow;
        File.WriteAllText(path, JsonSerializer.Serialize(job, Opts));
        return path;
    }

    /// <summary>Load a job from an .rcsj file.</summary>
    public AsBuiltJob Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AsBuiltJob>(json, Opts)
               ?? throw new InvalidOperationException($"Failed to deserialize job from {path}");
    }

    /// <summary>Enumerate all saved .rcsj files in the jobs directory.</summary>
    public IEnumerable<string> ListSavedJobs()
    {
        if (!Directory.Exists(JobsDir)) return Enumerable.Empty<string>();
        return Directory.GetFiles(JobsDir, "*.rcsj");
    }

    /// <summary>Delete a saved job file.</summary>
    public void Delete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>True if a prior autosave file exists for this job (crash restore).</summary>
    public bool HasCrashRestore(AsBuiltJob job)
        => File.Exists(AutosavePath(job));

    /// <summary>Write a crash-restore snapshot (called on short timer tick).</summary>
    public void Autosave(AsBuiltJob job)
    {
        try
        {
            Directory.CreateDirectory(JobsDir);
            File.WriteAllText(AutosavePath(job), JsonSerializer.Serialize(job, Opts));
        }
        catch { /* non-fatal — autosave must never crash the app */ }
    }

    /// <summary>Remove the crash-restore snapshot once a full save succeeds.</summary>
    public void ClearAutosave(AsBuiltJob job)
    {
        var p = AutosavePath(job);
        if (File.Exists(p)) File.Delete(p);
    }

    /// <summary>Returns the standard storage path for this job without saving.</summary>
    public static string DefaultPath(AsBuiltJob job)
        => Path.Combine(JobsDir, $"{job.JobId}.rcsj");

    private static string AutosavePath(AsBuiltJob job)
        => Path.Combine(JobsDir, $"{job.JobId}.autosave.rcsj");
}
