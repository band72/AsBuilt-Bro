using System;
using System.Collections.ObjectModel;
using RCS.Piping.Core.Models;

namespace RCS.Piping.Core.Workflow;

// ── Enumerations ─────────────────────────────────────────────────────────────

public enum WorkflowPhase
{
    Intake         = 0,
    PointsCleanup  = 1,
    Structures     = 2,
    PipeRuns       = 3,
    PartsMapping   = 4,
    Validation     = 5,
    Preview        = 6,
    Deliverables   = 7,
    ExportPackage  = 8
}

public enum MappingStatus
{
    Pending,
    Resolved,
    Skipped,
    Error
}

public enum CoordinateEnvironment
{
    Unknown,
    StatePlane,
    LocalGrid,
    AssumedLocal,
    Gps,
    LatLong
}

// ── Job Identity ──────────────────────────────────────────────────────────────

public class ProjectIdentity
{
    public string JobNumber       { get; set; } = string.Empty;
    public string ClientName      { get; set; } = string.Empty;
    public string County          { get; set; } = string.Empty;
    public string UtilityOwner    { get; set; } = string.Empty;
    public DateTime? FieldDate    { get; set; } = DateTime.Today;
    public string Drafter         { get; set; } = string.Empty;
    public string Checker         { get; set; } = string.Empty;
    public string Contractor      { get; set; } = string.Empty;
    public string Description     { get; set; } = string.Empty;
    public string Template        { get; set; } = string.Empty;
    public int    RevisionNumber  { get; set; } = 1;
}

// ── Parts Mapping ─────────────────────────────────────────────────────────────

public class PartMappingEntry
{
    public string AssetId          { get; set; } = string.Empty;
    public string DisplayName      { get; set; } = string.Empty;  // UI-friendly label
    public string DetectedDesc     { get; set; } = string.Empty;
    public string? ProposedPartKey { get; set; }
    public string  PartKey         { get; set; } = string.Empty;  // Resolved catalog key
    public string  Manufacturer    { get; set; } = string.Empty;
    public double  Confidence      { get; set; }     // 0.0 – 1.0
    public MappingStatus Status    { get; set; } = MappingStatus.Pending;
}

// ── Deliverable Readiness ─────────────────────────────────────────────────────

public enum DeliverableType { Dxf, PdfReport, LandXml, Pnezd, PartsReport, CertificationPackage }

public class DeliverableCard
{
    public string Type             { get; set; } = string.Empty;  // Free-form label (e.g. "DXF Drawing")
    public DeliverableType TypeEnum { get; set; } = DeliverableType.Dxf;
    public bool   IsEnabled           { get; set; } = true;
    public bool   IsBlocked           { get; set; }
    public int    BlockingErrorCount  { get; set; }
    public int    WarningCount        { get; set; }
    public string StatusMessage       { get; set; } = string.Empty;
}

// ── Root Job Container ────────────────────────────────────────────────────────

/// <summary>
/// Single source of truth for one as-built production session.
/// All engines, ViewModels, and export builders read from and write to this object.
/// </summary>
public class AsBuiltJob
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public Guid             JobId       { get; set; } = Guid.NewGuid();
    public ProjectIdentity  Identity    { get; set; } = new();
    public CoordinateEnvironment Environment { get; set; } = CoordinateEnvironment.Unknown;
    public DateTime         CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime         LastSaved   { get; set; } = DateTime.UtcNow;

    // ── Linked RCS Project ────────────────────────────────────────────────────
    /// <summary>GUID of the parent RCS.Cogo project (from AppDbContext.Projects).</summary>
    public string? LinkedProjectId { get; set; }

    // ── Survey Points (flat row list for the Points phase DataGrid) ───────────────
    public ObservableCollection<PointRow> PointRows { get; set; } = new();

    // ── Core Network ──────────────────────────────────────────────────────────
    public PipeNetwork Network { get; set; } = new();

    // ── Parts Mapping ─────────────────────────────────────────────────────────
    public ObservableCollection<PartMappingEntry> PartMappings { get; set; } = new();

    // ── Deliverable Targets ───────────────────────────────────────────────────
    public ObservableCollection<DeliverableCard> Deliverables { get; set; } = new()
    {
        new() { Type = "DXF Drawing",      TypeEnum = DeliverableType.Dxf },
        new() { Type = "PDF Report",       TypeEnum = DeliverableType.PdfReport },
        new() { Type = "LandXML",          TypeEnum = DeliverableType.LandXml,   IsEnabled = false },
        new() { Type = "PNEZD",            TypeEnum = DeliverableType.Pnezd },
        new() { Type = "Parts Report",     TypeEnum = DeliverableType.PartsReport }
    };

    // ── Pending Imports (paths queued by wizard for IntakeAnalysisEngine) ────
    public List<string> PendingImportPaths { get; set; } = new();

    // ── Workflow State ────────────────────────────────────────────────────────
    public WorkflowPhase CurrentPhase { get; set; } = WorkflowPhase.Intake;

    // ── Export History ────────────────────────────────────────────────────────
    public List<ExportRecord> ExportHistory { get; set; } = new();

    // ── Convenience Accessors ─────────────────────────────────────────────────
    public bool AllPartsMapped =>
        PartMappings.Count == 0 ||
        PartMappings.All(p => p.Status == MappingStatus.Resolved || p.Status == MappingStatus.Skipped);
}


public class ExportRecord
{
    public DateTime ExportedAt    { get; set; } = DateTime.UtcNow;
    public string   PackagePath   { get; set; } = string.Empty;
    public int      RevisionNumber { get; set; }
    public List<string> FilesGenerated { get; set; } = new();
}
