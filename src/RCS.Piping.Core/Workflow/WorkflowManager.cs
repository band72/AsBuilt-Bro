namespace RCS.Piping.Core.Workflow;

/// <summary>
/// Governs which phases are accessible, enforces hard QC gates before
/// transitions, and exposes the canonical display metadata for the
/// Workflow Navigator panel.
/// </summary>
public class WorkflowManager
{
    // ── Display Metadata ──────────────────────────────────────────────────────

    public static readonly IReadOnlyList<WorkflowStepInfo> Steps =
    [
        new(WorkflowPhase.Dashboard,     "0",  "Dashboard",     "📊"),
        new(WorkflowPhase.Intake,        "1",  "Intake",        "📥"),
        new(WorkflowPhase.PointsCleanup, "2",  "Points",        "📌"),
        new(WorkflowPhase.Structures,    "3",  "Structures",    "🏗"),
        new(WorkflowPhase.PipeRuns,      "4",  "Pipe Runs",     "〰"),
        new(WorkflowPhase.PartsMapping,  "5",  "Parts Mapping", "🔧"),
        new(WorkflowPhase.Validation,    "6",  "Validation",    "✅"),
        new(WorkflowPhase.Preview,       "7",  "Preview",       "👁"),
        new(WorkflowPhase.Deliverables,  "8",  "Deliverables",  "📦"),
        new(WorkflowPhase.ExportPackage, "9",  "Export",        "🚀"),
    ];

    // ── Phase Transition Gates ────────────────────────────────────────────────

    /// <summary>
    /// Checks whether the job satisfies all hard pre-conditions required
    /// to enter <paramref name="targetPhase"/>. Returns a list of blocking
    /// reasons; empty means the transition is allowed.
    /// </summary>
    public IReadOnlyList<string> GetTransitionBlockers(
        AsBuiltJob job, WorkflowPhase targetPhase) =>
        targetPhase switch
        {
            WorkflowPhase.PartsMapping =>
                job.Network.GetAllStructures().Any() || job.Network.GetAllRuns().Any()
                    ? []
                    : ["No structures or pipe runs have been defined yet."],

            WorkflowPhase.Validation =>
                job.AllPartsMapped
                    ? []
                    : ["Parts mapping is incomplete. All assets must be resolved or skipped before final validation."],

            WorkflowPhase.Preview =>
                GetValidationErrors(job).Count == 0
                    ? []
                    : GetValidationErrors(job),

            WorkflowPhase.Deliverables =>
                GetValidationErrors(job).Count == 0
                    ? []
                    : GetValidationErrors(job),

            WorkflowPhase.ExportPackage =>
                job.Identity.JobNumber.Length > 0
                    ? []
                    : ["Job Number is required before building an export package."],

            _ => []
        };

    /// <summary>
    /// Returns true only when no hard blockers exist for the target phase.
    /// </summary>
    public bool CanTransitionTo(AsBuiltJob job, WorkflowPhase targetPhase)
        => GetTransitionBlockers(job, targetPhase).Count == 0;

    // ── Export Gate ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true only when the job has zero validation errors.
    /// Warnings are permitted; they appear on individual deliverable cards.
    /// </summary>
    public bool IsExportReady(AsBuiltJob job)
        => GetValidationErrors(job).Count == 0;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<string> GetValidationErrors(AsBuiltJob job)
    {
        var engine = new ValidationEngine();
        var result = engine.Validate(job);
        return result.Issues
            .Where(i => i.Severity == IssueSeverity.Error)
            .Select(i => i.Message)
            .ToList();
    }
}

// ── Step Display Model ────────────────────────────────────────────────────────

public sealed record WorkflowStepInfo(
    WorkflowPhase Phase,
    string       StepNumber,
    string       Label,
    string       Icon);
