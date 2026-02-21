namespace RCS.Piping.Core.Models;

public class MaterialItem
{
    // Match fields
    public string PartKey { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string FeatureType { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;

    // Output fields
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty; // ManufacturerPartNo
    public string Year { get; set; } = string.Empty; // YearManufactured
    
    // Notes
    public string Notes { get; set; } = string.Empty;

    // Display for UI
    public string DisplayName => $"{Manufacturer} {Model} ({Size} {Material})";
    
    // For Project Schedule usage
    public int Quantity { get; set; } = 1;

    public void CopyFrom(MaterialItem other)
    {
        PartKey = other.PartKey;
        Discipline = other.Discipline;
        FeatureType = other.FeatureType;
        Size = other.Size;
        Material = other.Material;
        Manufacturer = other.Manufacturer;
        Model = other.Model;
        Year = other.Year;
        Notes = other.Notes;
    }
}
