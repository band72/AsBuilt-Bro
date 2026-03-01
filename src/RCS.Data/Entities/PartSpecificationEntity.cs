using System.ComponentModel.DataAnnotations;

namespace RCS.Data.Entities;

public class PartSpecificationEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string PartNumber { get; set; } = string.Empty;
    
    public double? OuterDiameter { get; set; }
    public double? NominalDiameter { get; set; }
    public double? PipeThickness { get; set; }
    public double? InnerDiameter { get; set; }
    public double? Deflection { get; set; }
    public string Note { get; set; } = string.Empty;
}
