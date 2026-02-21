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
    public string? Size { get; set; }
    public string? Material { get; set; }
    public int? Quantity { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNo { get; set; }
    public string? YearManufactured { get; set; }
    public string? Confidence { get; set; }
    public string? Source { get; set; }
    public string? Warning { get; set; }
    public string? Notes { get; set; }
}
