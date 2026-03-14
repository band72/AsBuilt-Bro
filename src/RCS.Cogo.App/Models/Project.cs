
namespace RCS.Cogo.App.Models;

public sealed class ProjectionSettings
{
    // Example: "EPSG:2236"
    public string EpsgCode { get; set; } = "";

    // Friendly label shown in UI
    public string Name { get; set; } = "";

    // "USFT" or "M"
    public string Units { get; set; } = "USFT";

    // If true, user explicitly set a projection (even if EPSG blank)
    public bool IsSet { get; set; } = false;
}

public sealed class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AvailNo { get; set; } = "";
    public string ProjectName { get; set; } = "New Project";
    public string SaveLocation { get; set; } = "";
    public string Utility { get; set; } = "";
    public string Units { get; set; } = "USFT";
    public int Revision { get; set; } = 1;
    public ProjectionSettings Projection { get; set; } = new();

    // Data lists for serialization
    public System.Collections.Generic.List<PointEntry> Points { get; set; } = new();
    public System.Collections.Generic.List<RCS.Piping.Core.Models.PipeRun> PipeRuns { get; set; } = new();
    public System.Collections.Generic.List<RCS.Piping.Core.Models.PipeStructure> Structures { get; set; } = new();
    // Material list
    public System.Collections.Generic.List<RCS.Piping.Core.Models.MaterialItem> Materials { get; set; } = new();

    // Project Metadata & Settings
    public ProjectSettings Settings { get; set; } = new();
    public ReportConfiguration ReportConfig { get; set; } = new();
    public System.Collections.Generic.List<Deliverable> Deliverables { get; set; } = new();
}


public class PointEntry
{
    public string Id { get; set; } = "";
    public double Northing { get; set; }
    public double Easting { get; set; }
    public double Elevation { get; set; }
    public string Description { get; set; } = "";
}
