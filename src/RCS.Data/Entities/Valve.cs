namespace RCS.Data.Entities;

public class Valve : InstalledAsset
{
    public string? Description { get; set; }
    public double? Northing { get; set; }
    public double? Easting { get; set; }
    public double? Elevation { get; set; }
    public string? Type { get; set; } // Gate, Check, etc.
    public string? OpenDirection { get; set; }
    public double? TurnsToOpen { get; set; }
    public double? NutElevation { get; set; }
}
