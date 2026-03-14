namespace RCS.Data.Entities;

public class PipeCrossing : InstalledAsset
{
    public string? Description { get; set; }
    public double? Northing { get; set; }
    public double? Easting { get; set; }
    public string? CrossingNumber { get; set; }
    public string? UpperPipeType { get; set; }
    public string? UpperPipeSize { get; set; }
    public double? FinishedGradeElevation { get; set; }
    public double? UpperPipeTopElevation { get; set; }
    public double? UpperCover { get; set; }
    public double? UpperPipeBottomElevation { get; set; }
    public string? LowerPipeType { get; set; }
    public string? LowerPipeSize { get; set; }
    public double? LowerPipeTopElevation { get; set; }
    public double? LowerCover { get; set; }
    public double? Separation { get; set; }
}
