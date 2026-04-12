using RCS.Piping.Core.Models;

namespace RCS.Piping.Core.Workflow;

// ── Severity & Issue Types ────────────────────────────────────────────────────

public enum IssueSeverity { Info, Warning, Error }

public enum IssueCategory
{
    Geometry,
    Coordinates,
    Structures,
    Runs,
    PartsMapping,
    Labels,
    Projection,
    ExportReadiness
}

public class ValidationIssue
{
    public IssueSeverity  Severity     { get; init; }
    public IssueCategory  Category     { get; init; }
    public string         Message      { get; init; } = string.Empty;
    public string?        TargetId     { get; init; }   // Structure/Run ID for "Zoom To"
    public string?        RuleName     { get; init; }
    public string?        SuggestedFix { get; init; }
    public bool           AutoFixable  { get; init; }

    public ValidationIssue(IssueSeverity severity, IssueCategory category,
                            string message, string? targetId = null,
                            string? ruleName = null, string? suggestedFix = null,
                            bool autoFixable = false)
    {
        Severity    = severity;
        Category    = category;
        Message     = message;
        TargetId    = targetId;
        RuleName    = ruleName;
        SuggestedFix = suggestedFix;
        AutoFixable = autoFixable;
    }
}

public class ValidationResult
{
    public IReadOnlyList<ValidationIssue> Issues { get; }
    public bool IsExportReady => Issues.All(i => i.Severity != IssueSeverity.Error);
    public int  ErrorCount    => Issues.Count(i => i.Severity == IssueSeverity.Error);
    public int  WarningCount  => Issues.Count(i => i.Severity == IssueSeverity.Warning);

    public ValidationResult(IEnumerable<ValidationIssue> issues)
        => Issues = issues.ToList().AsReadOnly();

    public static ValidationResult Empty => new(Enumerable.Empty<ValidationIssue>());
}

// ── Rule Interface ────────────────────────────────────────────────────────────

public interface IValidationRule
{
    string RuleName   { get; }
    IssueCategory Category { get; }

    /// <summary>Evaluate this rule against the current job state.</summary>
    IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job);
}

// ── Concrete Rules ────────────────────────────────────────────────────────────

/// <summary>
/// Detects pipe runs where the downstream invert is higher than the upstream invert,
/// indicating a reversed or impossible gravity-flow configuration.
/// </summary>
public sealed class ReversedFlowSlopeRule : IValidationRule
{
    public string RuleName    => "SLOPE_REVERSAL";
    public IssueCategory Category => IssueCategory.Runs;

    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        foreach (var run in job.Network.GetAllRuns())
        {
            if (run.InvertStart.HasValue && run.InvertEnd.HasValue
                && run.InvertEnd > run.InvertStart)
            {
                yield return new ValidationIssue(
                    IssueSeverity.Error,
                    IssueCategory.Runs,
                    $"Run {run.Id}: downstream invert ({run.InvertEnd:F2}) is higher than upstream ({run.InvertStart:F2}). Reversed gravity flow.",
                    targetId:     run.Id,
                    ruleName:     RuleName,
                    suggestedFix: "Swap InvertStart / InvertEnd or mark as pressure main.",
                    autoFixable:  true);
            }
        }
    }
}

/// <summary>
/// Flags structures with no rim elevation assigned (required for delivery).
/// </summary>
public sealed class MissingRimElevationRule : IValidationRule
{
    public string RuleName    => "MISSING_RIM";
    public IssueCategory Category => IssueCategory.Structures;

    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        foreach (var s in job.Network.GetAllStructures())
        {
            if (!s.RimElevation.HasValue)
                yield return new ValidationIssue(
                    IssueSeverity.Warning,
                    IssueCategory.Structures,
                    $"Structure {s.Id} has no rim elevation.",
                    targetId:  s.Id,
                    ruleName:  RuleName,
                    suggestedFix: "Assign rim from nearest survey point.");
        }
    }
}

/// <summary>
/// Flags parts that have not been resolved to a catalog entry.
/// </summary>
public sealed class UnmappedPartsRule : IValidationRule
{
    public string RuleName    => "UNMAPPED_PARTS";
    public IssueCategory Category => IssueCategory.PartsMapping;

    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        var pending = job.PartMappings
            .Where(p => p.Status == MappingStatus.Pending || p.Status == MappingStatus.Error)
            .ToList();

        if (pending.Count > 0)
            yield return new ValidationIssue(
                IssueSeverity.Error,
                IssueCategory.PartsMapping,
                $"{pending.Count} asset(s) have unresolved parts mappings and will block LandXML export.",
                ruleName:     RuleName,
                suggestedFix: "Complete the Parts Mapping step.");
    }
}

/// <summary>
/// Checks every run for a valid (> 0) diameter.
/// </summary>
public sealed class ZeroDiameterRule : IValidationRule
{
    public string RuleName    => "ZERO_DIAMETER";
    public IssueCategory Category => IssueCategory.Runs;

    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        foreach (var run in job.Network.GetAllRuns())
        {
            if (run.Diameter <= 0)
                yield return new ValidationIssue(
                    IssueSeverity.Error,
                    IssueCategory.Runs,
                    $"Run {run.Id}: diameter is zero or negative ({run.Diameter}).",
                    targetId: run.Id,
                    ruleName: RuleName);
        }
    }
}

/// <summary>
/// Detects disconnected runs whose endpoint point IDs have no matching structure.
/// </summary>
public sealed class DisconnectedRunRule : IValidationRule
{
    public string RuleName    => "DISCONNECTED_RUN";
    public IssueCategory Category => IssueCategory.Geometry;

    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        var structurePointIds = job.Network.GetAllStructures()
            .Select(s => s.PointId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var run in job.Network.GetAllRuns())
        {
            bool fromConnected = structurePointIds.Contains(run.FromPointId);
            bool toConnected   = structurePointIds.Contains(run.ToPointId);

            if (!fromConnected)
                yield return new ValidationIssue(
                    IssueSeverity.Warning,
                    IssueCategory.Geometry,
                    $"Run {run.Id}: upstream point '{run.FromPointId}' has no matching structure.",
                    targetId: run.Id, ruleName: RuleName);

            if (!toConnected)
                yield return new ValidationIssue(
                    IssueSeverity.Warning,
                    IssueCategory.Geometry,
                    $"Run {run.Id}: downstream point '{run.ToPointId}' has no matching structure.",
                    targetId: run.Id, ruleName: RuleName);
        }
    }
}

// ── Validation Engine ─────────────────────────────────────────────────────────

/// <summary>
/// Orchestrates the full battery of validation rules against an AsBuiltJob.
/// Can be executed from a background Task to avoid blocking the UI thread.
/// </summary>
public class ValidationEngine
{
    private readonly List<IValidationRule> _rules;

    public ValidationEngine() : this(DefaultRules()) { }

    public ValidationEngine(IEnumerable<IValidationRule> rules)
        => _rules = rules.ToList();

    /// <summary>Run all rules and return the aggregated result.</summary>
    public ValidationResult Validate(AsBuiltJob job)
    {
        var allIssues = _rules
            .SelectMany(r => r.Evaluate(job))
            .ToList();
        return new ValidationResult(allIssues);
    }

    /// <summary>Run only rules in a specific category (for targeted re-validation).</summary>
    public ValidationResult ValidateCategory(AsBuiltJob job, IssueCategory category)
    {
        var issues = _rules
            .Where(r => r.Category == category)
            .SelectMany(r => r.Evaluate(job))
            .ToList();
        return new ValidationResult(issues);
    }

    private static IEnumerable<IValidationRule> DefaultRules() =>
    [
        new ReversedFlowSlopeRule(),
        new MissingRimElevationRule(),
        new UnmappedPartsRule(),
        new ZeroDiameterRule(),
        new DisconnectedRunRule()
    ];
}
