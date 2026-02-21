using System.ComponentModel.DataAnnotations;

namespace RCS.Data.Entities;

public class MaterialEntity
{
    [Key]
    public int Id { get; set; }
    
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
}
