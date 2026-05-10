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
    public int            SourceLineNumber { get; init; }

    public ValidationIssue(IssueSeverity severity, IssueCategory category,
                            string message, string? targetId = null,
                            string? ruleName = null, string? suggestedFix = null,
                            bool autoFixable = false, int sourceLineNumber = 0)
    {
        Severity    = severity;
        Category    = category;
        Message     = message;
        TargetId    = targetId;
        RuleName    = ruleName;
        SuggestedFix = suggestedFix;
        AutoFixable = autoFixable;
        SourceLineNumber = sourceLineNumber;
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
                    autoFixable:  true,
                    sourceLineNumber: run.SourceLineNumber);
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
                    suggestedFix: "Assign rim from nearest survey point.",
                    sourceLineNumber: s.SourceLineNumber);
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
                    ruleName: RuleName,
                    sourceLineNumber: run.SourceLineNumber);
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
                    targetId: run.Id, ruleName: RuleName, suggestedFix: "Auto-create generic missing node structure.", autoFixable: true, sourceLineNumber: run.SourceLineNumber);

            if (!toConnected)
                yield return new ValidationIssue(
                    IssueSeverity.Warning,
                    IssueCategory.Geometry,
                    $"Run {run.Id}: downstream point '{run.ToPointId}' has no matching structure.",
                    targetId: run.Id, ruleName: RuleName, suggestedFix: "Auto-create generic missing node structure.", autoFixable: true, sourceLineNumber: run.SourceLineNumber);
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
        new DepthOfCoverRule(),
        new UnmappedPartsRule(),
        new ZeroDiameterRule(),
        new DisconnectedRunRule(),
        new PipeCrossingRule(),
        new ToleranceDeviationRule(),
        new MinimumCoverRule(),
        new ManningCapacityRule()
    ];
}
/// <summary>
/// Detects physical clashes between non-connected pipe runs where 3D vertical clearance is dangerously low (< 1.5 ft).
/// </summary>
public sealed class PipeCrossingRule : IValidationRule
{
    public string RuleName => "PIPE_CROSSING_CONFLICT";
    public IssueCategory Category => IssueCategory.Geometry;

    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        var runs = job.Network.GetAllRuns().ToList();
        var coords = job.PointRows.ToDictionary(r => r.PointId, r => r);

        for (int i = 0; i < runs.Count; i++)
        {
            var r1 = runs[i];
            if (!coords.TryGetValue(r1.FromPointId, out var p1Start) || !coords.TryGetValue(r1.ToPointId, out var p1End)) continue;
            
            double r1StartZ = r1.InvertStart ?? p1Start.Elevation;
            double r1EndZ   = r1.InvertEnd ?? p1End.Elevation;
            
            for (int j = i + 1; j < runs.Count; j++)
            {
                var r2 = runs[j];
                // Ignore physically connected runs at same node
                if (r1.FromPointId == r2.FromPointId || r1.ToPointId == r2.ToPointId || 
                    r1.FromPointId == r2.ToPointId || r1.ToPointId == r2.FromPointId)
                    continue;

                if (!coords.TryGetValue(r2.FromPointId, out var p2Start) || !coords.TryGetValue(r2.ToPointId, out var p2End)) continue;

                // 3D Volumetric Clearance Sampling
                double min3dClearance = double.MaxValue;
                
                double r2StartZ = r2.InvertStart ?? p2Start.Elevation;
                double r2EndZ   = r2.InvertEnd ?? p2End.Elevation;
                
                double r1DiamFt = r1.Diameter / 12.0;
                double r2DiamFt = r2.Diameter / 12.0;
                double requiredClearance = 3.0; // 3-foot exclusion buffer
                
                int samples = 10;
                for(int s = 0; s <= samples; s++)
                {
                    double t = (double)s / samples;
                    double x1 = p1Start.Easting + t * (p1End.Easting - p1Start.Easting);
                    double y1 = p1Start.Northing + t * (p1End.Northing - p1Start.Northing);
                    double z1 = r1StartZ + t * (r1EndZ - r1StartZ);
                    
                    // Nearest point on L2
                    double lenSq = Math.Pow(p2End.Easting - p2Start.Easting, 2) + Math.Pow(p2End.Northing - p2Start.Northing, 2) + Math.Pow(r2EndZ - r2StartZ, 2);
                    if (lenSq == 0) continue;
                    
                    double t2 = ((x1 - p2Start.Easting) * (p2End.Easting - p2Start.Easting) +
                                 (y1 - p2Start.Northing) * (p2End.Northing - p2Start.Northing) +
                                 (z1 - r2StartZ) * (r2EndZ - r2StartZ)) / lenSq;
                                 
                    t2 = Math.Max(0, Math.Min(1, t2));
                    double x2 = p2Start.Easting + t2 * (p2End.Easting - p2Start.Easting);
                    double y2 = p2Start.Northing + t2 * (p2End.Northing - p2Start.Northing);
                    double z2 = r2StartZ + t2 * (r2EndZ - r2StartZ);
                    
                    double centerDist = Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2) + Math.Pow(z1 - z2, 2));
                    double physicalClearance = centerDist - (r1DiamFt / 2.0) - (r2DiamFt / 2.0);
                    
                    min3dClearance = Math.Min(min3dClearance, physicalClearance);
                }

                if (min3dClearance < requiredClearance)
                {
                    yield return new ValidationIssue(
                        IssueSeverity.Warning,
                        IssueCategory.Geometry,
                        $"Volumetric Clash: Runs {r1.Id} ({r1.Type}) and {r2.Id} ({r2.Type}) breach the 3.0ft 3D clearance buffer (Physical Separation: {min3dClearance:F2} ft).",
                        targetId: r1.Id, ruleName: RuleName, suggestedFix: "Auto-trench invert offset to evade 3D buffer bounds.", autoFixable: true, sourceLineNumber: r1.SourceLineNumber);
                }
            }
        }
    }
}

/// <summary>
/// Verifies As-Built pipeline runs fall within acceptable threshold deviations against the Official Design Baseline.
/// </summary>
public sealed class ToleranceDeviationRule : IValidationRule
{
    public string RuleName => "DESIGN_DEVIATION";
    public IssueCategory Category => IssueCategory.Geometry;
    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        if (job.DesignBaseline == null) yield break;
        foreach (var run in job.Network.GetAllRuns())
        {
            if (job.DesignBaseline.Network.Runs.TryGetValue(run.Id, out var designRun))
            {
                double devZStart = Math.Abs((run.InvertStart ?? 0) - (designRun.InvertStart ?? 0));
                double devZEnd = Math.Abs((run.InvertEnd ?? 0) - (designRun.InvertEnd ?? 0));
                if (devZStart > 0.5 || devZEnd > 0.5)
                {
                    yield return new ValidationIssue(
                        IssueSeverity.Warning,
                        IssueCategory.Geometry,
                        $"Run {run.Id} exceeds 0.5ft vertical tolerance vs design! Start: +{devZStart:F2}ft, End: +{devZEnd:F2}ft.",
                        targetId: run.Id, ruleName: RuleName);
                }
            }
        }
    }
}

/// <summary>
/// Interpolates over the imported Topographic grid data to verify that gravity main structures do not violate the 36-inch minimum cover statute.
/// </summary>
public sealed class MinimumCoverRule : IValidationRule
{
    public string RuleName => "MINIMUM_COVER";
    public IssueCategory Category => IssueCategory.Geometry;
    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        if (job.BaseSurface == null || job.BaseSurface.Points.Count == 0) yield break;
        foreach (var st in job.Network.GetAllStructures())
        {
            if (!st.RimElevation.HasValue && !string.IsNullOrEmpty(st.PointId))
            {
                var ptRow = job.PointRows.FirstOrDefault(p => p.PointId == st.PointId);
                if (ptRow != null)
                {
                    double groundZ = job.BaseSurface.InterpolateElevation(ptRow.Easting, ptRow.Northing);
                    // Determine attached invert depths
                    var attachedRuns = job.Network.GetAllRuns().Where(r => r.FromPointId == st.PointId || r.ToPointId == st.PointId).ToList();
                    foreach (var ar in attachedRuns)
                    {
                        var inv = ar.FromPointId == st.PointId ? ar.InvertStart : ar.InvertEnd;
                        if (inv.HasValue && (groundZ - inv.Value) < 3.0)
                        {
                            yield return new ValidationIssue(
                                IssueSeverity.Error,
                                IssueCategory.Geometry,
                                $"Structure {st.Id} lacks minimum cover! GroundZ: {groundZ:F2}', InvertZ: {inv.Value:F2}' (Cover: {(groundZ - inv.Value):F2}ft < 3.0ft)",
                                targetId: st.Id, ruleName: RuleName);
                        }
                    }
                }
            }
        }
    }
}


/// <summary>
/// Calculates the maximum theoretical flow capacity (CFS) for gravity mains using Manning's Equation.
/// Identifies undersized runs via capacity bottlenecks.
/// </summary>
public sealed class ManningCapacityRule : IValidationRule
{
    public string RuleName => "CAPACITY_BOTTLENECK";
    public IssueCategory Category => IssueCategory.Geometry;
    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        foreach (var run in job.Network.GetAllRuns())
        {
            if (run.Diameter <= 0 || !run.InvertStart.HasValue || !run.InvertEnd.HasValue) continue;
            
            // Standard SDR-35 PVC roughness
            double n = 0.013;
            double diamFt = run.Diameter / 12.0;
            double area = Math.PI * Math.Pow(diamFt / 2.0, 2);
            double hydRadius = diamFt / 4.0; // full flow condition
            
            double dz = Math.Abs(run.InvertStart.Value - run.InvertEnd.Value);
            double dx = run.ComputedLength > 0 ? run.ComputedLength : 0.01;
            double slope = dz / dx;
            if (slope <= 0) continue;
            
            double velocity = (1.49 / n) * Math.Pow(hydRadius, 2.0/3.0) * Math.Sqrt(slope);
            double capacityCfs = area * velocity;
            run.MaxFlowCfs = capacityCfs;
            
            if (capacityCfs < 5.0 && run.Type.IndexOf("MAIN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                yield return new ValidationIssue(
                    IssueSeverity.Warning,
                    IssueCategory.Geometry,
                    $"Mainline Run {run.Id} is severely undersized (Max Capacity: {capacityCfs:F2} CFS). Expect surcharging.",
                    targetId: run.Id, ruleName: RuleName);
            }
        }
    }
}

public class DepthOfCoverRule : IValidationRule
{
    public string RuleName => "DEPTH_OF_COVER";
    public IssueCategory Category => IssueCategory.Geometry;

    public IEnumerable<ValidationIssue> Evaluate(AsBuiltJob job)
    {
        if (job.BaseSurface == null || job.BaseSurface.Points.Count == 0) yield break;

        var ptDict = job.PointRows.ToDictionary(p => p.PointId, p => p);
        foreach (var run in job.Network.GetAllRuns())
        {
            if (!run.InvertStart.HasValue || !run.InvertEnd.HasValue) continue;
            if (run.FromPointId == null || !ptDict.TryGetValue(run.FromPointId, out var fromPt)) continue;
            if (run.ToPointId == null || !ptDict.TryGetValue(run.ToPointId, out var toPt)) continue;

            // Sample midpoint parameter (could be upgraded to full 10-point volumetric sampling)
            double midE = (fromPt.Easting + toPt.Easting) / 2.0;
            double midN = (fromPt.Northing + toPt.Northing) / 2.0;
            double topOfPipeZ = (run.InvertStart.Value + run.InvertEnd.Value) / 2.0 + (run.Diameter / 12.0);
            
            double surfaceZ = job.BaseSurface.InterpolateElevation(midE, midN);
            double cover = surfaceZ - topOfPipeZ;

            if (cover < 3.0) // 36 inches legal minimum
            {
                yield return new ValidationIssue(
                    IssueSeverity.Error,
                    IssueCategory.Geometry,
                    $"Pipe Run {run.Id} violates 36-inch Depth of Cover constraint. (Actual Cover: {cover:F2} ft)",
                    targetId: run.Id, ruleName: RuleName);
            }
        }
    }
}

