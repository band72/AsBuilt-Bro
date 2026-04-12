using RCS.Piping.Core.Models;
using RCS.Piping.Core.Workflow;
using Xunit;

namespace RCS.Cogo.Core.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// Validation Engine Tests
// Tests confirm each concrete IValidationRule fires correctly and that
// the engine correctly aggregates results. No WPF or DB dependencies.
// ─────────────────────────────────────────────────────────────────────────────

public class ValidationEngineTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AsBuiltJob EmptyJob() => new();

    private static AsBuiltJob JobWithRuns(params (string id, double invertStart, double invertEnd)[] runs)
    {
        var job = EmptyJob();
        foreach (var (id, invertStart, invertEnd) in runs)
        {
            var run = new PipeRun { Id = id, Diameter = 12, Material = "PVC",
                                    InvertStart = invertStart, InvertEnd = invertEnd };
            job.Network.AddRun(run);
        }
        return job;
    }

    private static AsBuiltJob JobWithStructures(params (string id, string pointId, double? rim)[] structs)
    {
        var job = EmptyJob();
        foreach (var (id, pointId, rim) in structs)
            job.Network.AddStructure(new PipeStructure { Id = id, PointId = pointId, RimElevation = rim });
        return job;
    }

    // ── ReversedFlowSlopeRule ─────────────────────────────────────────────────

    [Fact]
    public void ReversedFlow_DownstreamHigherThanUpstream_ProducesError()
    {
        var job = JobWithRuns(("R1", 50.0, 55.0));   // downstream > upstream → error
        var result = new ValidationEngine().Validate(job);

        Assert.Equal(1, result.ErrorCount);
        Assert.Contains(result.Issues, i =>
            i.Severity == IssueSeverity.Error &&
            i.RuleName == "SLOPE_REVERSAL");
    }

    [Fact]
    public void ReversedFlow_CorrectSlope_ProducesNoError()
    {
        var job = JobWithRuns(("R1", 55.0, 50.0));   // upstream > downstream → valid
        var result = new ValidationEngine().Validate(job);

        Assert.DoesNotContain(result.Issues, i => i.RuleName == "SLOPE_REVERSAL");
    }

    [Fact]
    public void ReversedFlow_NullInverts_NoError()
    {
        var job = EmptyJob();
        job.Network.AddRun(new PipeRun
            { Id = "R1", Diameter = 12, InvertStart = null, InvertEnd = null });
        var result = new ValidationEngine().Validate(job);

        Assert.DoesNotContain(result.Issues, i => i.RuleName == "SLOPE_REVERSAL");
    }

    [Fact]
    public void ReversedFlow_MultipleRuns_ReportsEachViolation()
    {
        var job = JobWithRuns(
            ("R1", 50.0, 55.0),  // reversed
            ("R2", 60.0, 70.0),  // reversed
            ("R3", 80.0, 75.0)); // correct

        var result = new ValidationEngine().Validate(job);
        Assert.Equal(2, result.Issues.Count(i => i.RuleName == "SLOPE_REVERSAL"));
    }

    // ── MissingRimElevationRule ───────────────────────────────────────────────

    [Fact]
    public void MissingRim_NullRim_ProducesWarning()
    {
        var job = JobWithStructures(("S1", "PT1", null));
        var result = new ValidationEngine().Validate(job);

        Assert.Equal(1, result.WarningCount);
        Assert.Contains(result.Issues, i =>
            i.Severity == IssueSeverity.Warning &&
            i.RuleName == "MISSING_RIM");
    }

    [Fact]
    public void MissingRim_RimAssigned_NoWarning()
    {
        var job = JobWithStructures(("S1", "PT1", 42.5));
        var result = new ValidationEngine().Validate(job);

        Assert.DoesNotContain(result.Issues, i => i.RuleName == "MISSING_RIM");
    }

    // ── ZeroDiameterRule ──────────────────────────────────────────────────────

    [Fact]
    public void ZeroDiameter_ZeroValue_ProducesError()
    {
        var job = EmptyJob();
        job.Network.AddRun(new PipeRun { Id = "R1", Diameter = 0 });
        var result = new ValidationEngine().Validate(job);

        Assert.Contains(result.Issues, i =>
            i.Severity == IssueSeverity.Error &&
            i.RuleName == "ZERO_DIAMETER");
    }

    [Fact]
    public void ZeroDiameter_NegativeValue_ProducesError()
    {
        var job = EmptyJob();
        job.Network.AddRun(new PipeRun { Id = "R1", Diameter = -1 });
        var result = new ValidationEngine().Validate(job);

        Assert.Contains(result.Issues, i => i.RuleName == "ZERO_DIAMETER");
    }

    [Fact]
    public void ZeroDiameter_ValidDiameter_Passes()
    {
        var job = EmptyJob();
        job.Network.AddRun(new PipeRun { Id = "R1", Diameter = 8 });
        var result = new ValidationEngine().Validate(job);

        Assert.DoesNotContain(result.Issues, i => i.RuleName == "ZERO_DIAMETER");
    }

    // ── UnmappedPartsRule ─────────────────────────────────────────────────────

    [Fact]
    public void UnmappedParts_PendingEntry_ProducesError()
    {
        var job = EmptyJob();
        job.PartMappings.Add(new PartMappingEntry
            { AssetId = "A1", Status = MappingStatus.Pending });

        var result = new ValidationEngine().Validate(job);

        Assert.Contains(result.Issues, i =>
            i.Severity == IssueSeverity.Error &&
            i.RuleName == "UNMAPPED_PARTS");
    }

    [Fact]
    public void UnmappedParts_AllResolved_NoProblem()
    {
        var job = EmptyJob();
        job.PartMappings.Add(new PartMappingEntry
            { AssetId = "A1", Status = MappingStatus.Resolved });

        var result = new ValidationEngine().Validate(job);

        Assert.DoesNotContain(result.Issues, i => i.RuleName == "UNMAPPED_PARTS");
    }

    [Fact]
    public void UnmappedParts_SkippedCountsAsResolved()
    {
        var job = EmptyJob();
        job.PartMappings.Add(new PartMappingEntry
            { AssetId = "A1", Status = MappingStatus.Skipped });

        var result = new ValidationEngine().Validate(job);

        Assert.DoesNotContain(result.Issues, i => i.RuleName == "UNMAPPED_PARTS");
    }

    // ── DisconnectedRunRule ───────────────────────────────────────────────────

    [Fact]
    public void DisconnectedRun_MissingStructureMapping_ProducesWarning()
    {
        var job = EmptyJob();
        job.Network.AddRun(new PipeRun
            { Id = "R1", Diameter = 12, FromPointId = "PT_ORPHAN", ToPointId = "PT_ALSO_ORPHAN" });
        // No structures added → both endpoints disconnected

        var result = new ValidationEngine().Validate(job);

        Assert.Equal(2, result.Issues.Count(i =>
            i.Severity == IssueSeverity.Warning &&
            i.RuleName == "DISCONNECTED_RUN"));
    }

    [Fact]
    public void DisconnectedRun_FullyConnected_NoWarning()
    {
        var job = EmptyJob();
        job.Network.AddStructure(new PipeStructure { Id = "S1", PointId = "PT1" });
        job.Network.AddStructure(new PipeStructure { Id = "S2", PointId = "PT2" });
        job.Network.AddRun(new PipeRun
            { Id = "R1", Diameter = 12, FromPointId = "PT1", ToPointId = "PT2" });

        var result = new ValidationEngine().Validate(job);

        Assert.DoesNotContain(result.Issues, i => i.RuleName == "DISCONNECTED_RUN");
    }

    // ── ExportReadiness ───────────────────────────────────────────────────────

    [Fact]
    public void IsExportReady_NoErrors_ReturnsTrue()
    {
        var result = new ValidationEngine().Validate(EmptyJob());
        Assert.True(result.IsExportReady);
    }

    [Fact]
    public void IsExportReady_WithErrors_ReturnsFalse()
    {
        var job = JobWithRuns(("R_BAD", 10.0, 15.0));  // reversed slope error
        var result = new ValidationEngine().Validate(job);
        Assert.False(result.IsExportReady);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// WorkflowManager Transition Gate Tests
// ─────────────────────────────────────────────────────────────────────────────

public class WorkflowManagerTests
{
    private readonly WorkflowManager _wm = new();

    [Fact]
    public void PartsMapping_NoNetworkElements_IsBlocked()
    {
        var job = new AsBuiltJob();   // empty network
        var blockers = _wm.GetTransitionBlockers(job, WorkflowPhase.PartsMapping);
        Assert.NotEmpty(blockers);
    }

    [Fact]
    public void PartsMapping_HasStructures_IsAllowed()
    {
        var job = new AsBuiltJob();
        job.Network.AddStructure(new PipeStructure { Id = "S1", PointId = "PT1" });
        var blockers = _wm.GetTransitionBlockers(job, WorkflowPhase.PartsMapping);
        Assert.Empty(blockers);
    }

    [Fact]
    public void Validation_UnmappedParts_IsBlocked()
    {
        var job = new AsBuiltJob();
        job.PartMappings.Add(new PartMappingEntry
            { AssetId = "A1", Status = MappingStatus.Pending });

        var blockers = _wm.GetTransitionBlockers(job, WorkflowPhase.Validation);
        Assert.NotEmpty(blockers);
    }

    [Fact]
    public void Validation_AllMapped_IsAllowed()
    {
        var job = new AsBuiltJob();
        job.PartMappings.Add(new PartMappingEntry
            { AssetId = "A1", Status = MappingStatus.Resolved });

        var blockers = _wm.GetTransitionBlockers(job, WorkflowPhase.Validation);
        Assert.Empty(blockers);
    }

    [Fact]
    public void Export_MissingJobNumber_IsBlocked()
    {
        var job = new AsBuiltJob();
        job.Identity.JobNumber = string.Empty;
        var blockers = _wm.GetTransitionBlockers(job, WorkflowPhase.ExportPackage);
        Assert.NotEmpty(blockers);
    }

    [Fact]
    public void Export_WithJobNumber_IsAllowed()
    {
        var job = new AsBuiltJob();
        job.Identity.JobNumber = "JEA-2026-001";
        var blockers = _wm.GetTransitionBlockers(job, WorkflowPhase.ExportPackage);
        Assert.Empty(blockers);
    }

    [Fact]
    public void CanTransitionTo_DelegatesToBlockerList()
    {
        var job = new AsBuiltJob(); // empty → PartsMapping blocked
        Assert.False(_wm.CanTransitionTo(job, WorkflowPhase.PartsMapping));
    }

    [Fact]
    public void AllPhases_HaveDisplayMetadata()
    {
        // Ensure no phase is missing from the navigator display list
        var defined = WorkflowManager.Steps.Select(s => s.Phase).ToHashSet();
        foreach (WorkflowPhase phase in Enum.GetValues<WorkflowPhase>())
            Assert.Contains(phase, defined);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// AsBuiltJob Model Tests
// ─────────────────────────────────────────────────────────────────────────────

public class AsBuiltJobTests
{
    [Fact]
    public void AllPartsMapped_EmptyList_ReturnsTrue()
    {
        var job = new AsBuiltJob();
        Assert.True(job.AllPartsMapped);
    }

    [Fact]
    public void AllPartsMapped_PendingEntry_ReturnsFalse()
    {
        var job = new AsBuiltJob();
        job.PartMappings.Add(new PartMappingEntry { Status = MappingStatus.Pending });
        Assert.False(job.AllPartsMapped);
    }

    [Fact]
    public void AllPartsMapped_AllResolvedOrSkipped_ReturnsTrue()
    {
        var job = new AsBuiltJob();
        job.PartMappings.Add(new PartMappingEntry { Status = MappingStatus.Resolved });
        job.PartMappings.Add(new PartMappingEntry { Status = MappingStatus.Skipped });
        Assert.True(job.AllPartsMapped);
    }

    [Fact]
    public void DefaultDeliverables_ContainsDxfAndPdfAndPnezd()
    {
        var job = new AsBuiltJob();
        var types = job.Deliverables.Select(d => d.TypeEnum).ToHashSet();
        Assert.Contains(DeliverableType.Dxf,       types);
        Assert.Contains(DeliverableType.PdfReport,  types);
        Assert.Contains(DeliverableType.Pnezd,      types);
    }

    [Fact]
    public void NewJob_HasUniqueGuid()
    {
        var j1 = new AsBuiltJob();
        var j2 = new AsBuiltJob();
        Assert.NotEqual(j1.JobId, j2.JobId);
    }
}
