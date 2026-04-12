namespace RCS.Piping.Core.Engines;

// ─────────────────────────────────────────────────────────────────────────────
// Shared DTOs used by IntakeAnalysisEngine and the WPF ViewModel.
// Defined in RCS.Piping.Core so the engine has no dependency on the WPF layer.
// ─────────────────────────────────────────────────────────────────────────────

public enum IntakeFileType { Pnezd, CogoScript, JeaExcel, Dxf }

public class IntakeReport
{
    public int    PointsLoaded    { get; set; }
    public int    RunsLoaded      { get; set; }
    public int    StructuresFound { get; set; }
    public int    Warnings        { get; set; }
    public string Summary         { get; set; } = "No file imported yet.";
    public bool   Success         { get; set; }
}
