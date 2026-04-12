using RCS.Piping.Core.Workflow;

namespace RCS.Piping.Core.Services;

/// <summary>
/// Creates and manages job revisions.
/// A revision is a deep-copy of the current job with an incremented RevisionNumber.
/// </summary>
public class RevisionService
{
    private readonly JobPersistenceService _persist;

    public RevisionService(JobPersistenceService persist)
    {
        _persist = persist;
    }

    /// <summary>
    /// Saves the current job under a new revision number.
    /// Returns the new job (the caller should replace the active job with this).
    /// </summary>
    public AsBuiltJob CreateRevision(AsBuiltJob current)
    {
        // Persist the current state first so it's not lost
        _persist.Save(current);

        // Deep-copy via JSON roundtrip
        var json = System.Text.Json.JsonSerializer.Serialize(current);
        var next  = System.Text.Json.JsonSerializer.Deserialize<AsBuiltJob>(json)
                    ?? throw new InvalidOperationException("Revision copy failed.");

        // Bump identity
        next.JobId                   = Guid.NewGuid();
        next.Identity.RevisionNumber = current.Identity.RevisionNumber + 1;
        next.CreatedAt               = DateTime.UtcNow;
        next.LastSaved               = DateTime.UtcNow;

        // Clear export history so the new rev starts clean
        next.ExportHistory.Clear();

        _persist.Save(next);
        return next;
    }

    /// <summary>
    /// Lists all revision files for a given base job number (all revisions share the same JobNumber prefix).
    /// Returns paths sorted by RevisionNumber ascending.
    /// </summary>
    public IEnumerable<string> ListRevisions(string jobNumber)
        => _persist.ListSavedJobs()
                   .Where(p => System.IO.Path.GetFileName(p).StartsWith(jobNumber, StringComparison.OrdinalIgnoreCase))
                   .OrderBy(p => p);
}
