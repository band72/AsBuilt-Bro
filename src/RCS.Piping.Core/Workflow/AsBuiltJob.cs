using System;
using System.Collections.ObjectModel;
using RCS.Piping.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RCS.Piping.Core.Workflow;

// ── Enumerations ─────────────────────────────────────────────────────────────

public enum WorkflowPhase
{
    Dashboard      = -1,
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

public class PartMappingEntry : INotifyPropertyChanged
{
    private string _proposedPartKey = string.Empty;
    private string _manufacturer = string.Empty;
    private double _nominalDiameter = 0.0;
    private string _partMaterial = "PVC";
    private string _sdrClass = string.Empty;
    private string _notes = string.Empty;
    private MappingStatus _status = MappingStatus.Pending;

    public string AssetId          { get; set; } = string.Empty;
    public string DisplayName      { get; set; } = string.Empty;
    public string DetectedDesc     { get; set; } = string.Empty;
    
    public string ProposedPartKey 
    { 
        get => _proposedPartKey; 
        set { _proposedPartKey = value; OnPC(); } 
    }
    
    public string PartKey { get; set; } = string.Empty;
    
    public string Manufacturer    
    { 
        get => _manufacturer; 
        set { _manufacturer = value; OnPC(); } 
    }

    public double NominalDiameter 
    { 
        get => _nominalDiameter; 
        set { _nominalDiameter = value; OnPC(); } 
    }

    public string PartMaterial    
    { 
        get => _partMaterial; 
        set { _partMaterial = value; OnPC(); } 
    }

    public string SDRClass        
    { 
        get => _sdrClass; 
        set { _sdrClass = value; OnPC(); } 
    }

    public string Notes           
    { 
        get => _notes; 
        set { _notes = value; OnPC(); } 
    }

    public double Confidence { get; set; }

    public MappingStatus Status    
    { 
        get => _status; 
        set { _status = value; OnPC(); } 
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPC([CallerMemberName] string? n = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
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

    // ── Advanced Computations ──────────────────────────────────────────────────
    public RCS.Piping.Core.Models.TopographicSurface? BaseSurface { get; set; }
    public AsBuiltJob? DesignBaseline { get; set; }

    // ── Parts Mapping ─────────────────────────────────────────────────────────
    public ObservableCollection<PartMappingEntry> PartMappings { get; set; } = new();

    // ── Deliverable Targets ───────────────────────────────────────────────────
    public ObservableCollection<DeliverableCard> Deliverables { get; set; } = new()
    {
        new() { Type = "DXF Drawing",      TypeEnum = DeliverableType.Dxf },
        new() { Type = "PDF Report",       TypeEnum = DeliverableType.PdfReport },
        new() { Type = "LandXML",          TypeEnum = DeliverableType.LandXml,   IsEnabled = true },
        new() { Type = "PNEZD",            TypeEnum = DeliverableType.Pnezd },
        new() { Type = "Parts Report",     TypeEnum = DeliverableType.PartsReport }
    };

    // ── Pending Imports (paths queued by wizard for IntakeAnalysisEngine) ────
    public List<string> PendingImportPaths { get; set; } = new();

    // ── Workflow State ────────────────────────────────────────────────────────
    public WorkflowPhase CurrentPhase { get; set; } = WorkflowPhase.Intake;

    // ── Export History ────────────────────────────────────────────────────────
    public List<ExportRecord> ExportHistory { get; set; } = new();

    // ── Immutable Audit Trail ─────────────────────────────────────────────────
    public List<AuditEntry> AuditLog { get; set; } = new();

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

public class AuditEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string User { get; set; } = Environment.UserName;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

