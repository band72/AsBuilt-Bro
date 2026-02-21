using System.ComponentModel.DataAnnotations;

namespace RCS.Data.Entities;

public class ProjectEntity
{
    [Key]
    public string ProjectId { get; set; } = string.Empty;
    
    [Required]
    public string ProjectNumber { get; set; } = string.Empty;
    
    public string? ProjectName { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
