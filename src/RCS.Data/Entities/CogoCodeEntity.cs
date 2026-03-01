using System.ComponentModel.DataAnnotations;

namespace RCS.Data.Entities;

public class CogoCodeEntity
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string LocalCode { get; set; } = string.Empty;
    
    public string SystemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Block { get; set; }
}
