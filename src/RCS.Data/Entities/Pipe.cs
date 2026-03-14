namespace RCS.Data.Entities;

public class Pipe : InstalledAsset
{
    public string? Description { get; set; }
    public double? NorthingStart { get; set; }
    public double? EastingStart { get; set; }
    public double? NorthingEnd { get; set; }
    public double? EastingEnd { get; set; }
    public double? InvertStart { get; set; }
    public double? InvertEnd { get; set; }
    public double? GradeElevationAtInvertStart { get; set; }
    public double? GradeElevationAtInvertEnd { get; set; }
    public double? Diameter { get; set; }
}
