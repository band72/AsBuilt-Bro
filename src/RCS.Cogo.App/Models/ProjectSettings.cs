
namespace RCS.Cogo.App.Models;

public class Deliverable
{
    public string Name { get; set; } = "";
    public bool IsRequired { get; set; } = true;
    public bool IsCompleted { get; set; } = false;
    public string OutputPath { get; set; } = "";
}

public class ProjectSettings
{
    public bool AutoSave { get; set; } = true;
    public int AutoSaveIntervalMinutes { get; set; } = 5;
    
    // Default Deliverables Requirement
    public bool RequirePdfReport { get; set; } = true;
    public bool RequireLandXml { get; set; } = true;
    public bool RequireCsv { get; set; } = true;
}

public class ReportConfiguration
{
    public bool ExportWater { get; set; } = true;
    public string WaterSheetName { get; set; } = "Water";
    
    public bool ExportSewer { get; set; } = true;
    public string SewerSheetName { get; set; } = "Sewer"; // Sanitary

    public bool ExportStorm { get; set; } = true;    
    public string StormSheetName { get; set; } = "Storm";

    public bool ExportGas { get; set; } = true;
    public string GasSheetName { get; set; } = "Gas";
    
    public bool IncludeNullValues { get; set; } = false;
    
    // Bearing Format: DMS (Degrees Minutes Seconds) or DD (Decimal Degrees)
    public string BearingFormat { get; set; } = "DMS"; // "DMS" or "DD"
}
