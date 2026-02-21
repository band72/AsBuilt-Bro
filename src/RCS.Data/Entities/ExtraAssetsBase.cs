using RCS.Data.Entities;

namespace RCS.Data.Entities;

public class Hydrant : InstalledAsset
{
    public string? Description { get; set; }
    public double? Northing { get; set; }
    public double? Easting { get; set; }
    public double? Elevation { get; set; }
}

public class LocateBox : InstalledAsset
{
    public string? Description { get; set; }
    public double? Northing { get; set; }
    public double? Easting { get; set; }
    public double? Elevation { get; set; }
}
