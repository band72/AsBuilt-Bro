namespace RCS.Data.Entities;

public class Meter : InstalledAsset
{
    public string? Description { get; set; }
    public double? Northing { get; set; }
    public double? Easting { get; set; }
    public double? Elevation { get; set; }
}
