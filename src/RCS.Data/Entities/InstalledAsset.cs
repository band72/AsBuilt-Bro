namespace RCS.Data.Entities;

public abstract class InstalledAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ProjectId { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public string? SourceSheetRowIndex { get; set; }

    // JEA Common Columns
    public string? PartKey { get; set; }
    public string? Discipline { get; set; }
    public string? FeatureType { get; set; }
    public string? Subtype { get; set; }
    public string? FacilityOwner { get; set; }
    public string? Size { get; set; }
    public string? SizeSecondary { get; set; }
    public string? Material { get; set; }
    public string? PipeClass { get; set; }
    public string? LiningManufacturer { get; set; }
    public string? LiningMaterial { get; set; }
    public string? Orientation { get; set; }
    public string? PipeRole { get; set; }
    public string? RfidBarcode { get; set; }
    public string? DropType { get; set; }
    public string? InvertElevationsWithDirections { get; set; }
    public string? ExteriorJointTapeType { get; set; }
    public string? ExteriorJointTapeManufacturer { get; set; }
    public int? Quantity { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNo { get; set; }
    public string? YearManufactured { get; set; }
    public string? Confidence { get; set; }
    public string? Source { get; set; }
    public string? Warning { get; set; }
    public string? Notes { get; set; }

    // Dimensions & Elevations (Added for Invert Estimation)
    public double? TopOutsideWallElev { get; set; }
    public double? OuterWallThicknessTop { get; set; }
    public double? InnerDiameter { get; set; }
    public double? AdjustedInvert { get; set; }
    
    // Computed in C# 
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public double? EstimatedInvert 
    {
        get 
        {
             if (TopOutsideWallElev.HasValue && InnerDiameter.HasValue)
             {
                 double thickness = OuterWallThicknessTop ?? 0.0;
                 return Math.Round(TopOutsideWallElev.Value - thickness - InnerDiameter.Value, 2);
             }
             return null;
        }
    }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public double? FinalInvert => AdjustedInvert ?? EstimatedInvert;
}
