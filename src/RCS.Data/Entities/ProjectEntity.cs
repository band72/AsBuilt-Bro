using System.ComponentModel.DataAnnotations;

namespace RCS.Data.Entities;

public class ProjectEntity
{
    [Key]
    public string ProjectId { get; set; } = string.Empty;
    
    [Required]
    public string ProjectNumber { get; set; } = string.Empty;
    
    public string? ProjectName { get; set; }
    public string? County { get; set; }
    public string? Hyperlink { get; set; }
    public string? AsBuiltDate { get; set; }
    public string? DataSource { get; set; }
    public string? AvailabilityNumber { get; set; }
    public string? CapitalProjectNumber { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
