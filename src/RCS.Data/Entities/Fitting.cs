namespace RCS.Data.Entities;

public class Fitting : InstalledAsset
{
    public string? Description { get; set; }
    public double? Northing { get; set; }
    public double? Easting { get; set; }
    public double? Elevation { get; set; } // May vary, but typically singular point
    public string? Type { get; set; } // e.g. 45 Bend
}
