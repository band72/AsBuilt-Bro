namespace RCS.Data.Entities;

public class PipeCrossing : InstalledAsset
{
    public string? Description { get; set; }
    public double? Northing { get; set; }
    public double? Easting { get; set; }
    // Add columns here matching Excel
}
