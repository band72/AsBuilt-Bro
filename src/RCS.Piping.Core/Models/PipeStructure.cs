namespace RCS.Piping.Core.Models;

public class PipeStructure
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Link to Cogo Point ID
    public string PointId { get; set; } = string.Empty;

    public string Type { get; set; } = "Structure";
    
    // Elevations
    public double? RimElevation { get; set; }
    public double? InvertIn { get; set; }
    public double? InvertOut { get; set; }
    
    public override string ToString() => $"Structure {Id} (Pt:{PointId}) Type:{Type}";
}
