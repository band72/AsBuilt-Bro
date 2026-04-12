namespace RCS.Piping.Core.Models;

public class PipeRun
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PartKey { get; set; } = string.Empty;
    public string Type { get; set; } = "Generic";
    public string FigureName { get; set; } = string.Empty;

    // Connectivity (References Cogo Point IDs)
    public string FromPointId { get; set; } = string.Empty;
    public string ToPointId { get; set; } = string.Empty;

    // Geometry
    public double Diameter { get; set; }
    public string Material { get; set; } = string.Empty;
    public double? InvertStart { get; set; }
    public double? InvertEnd { get; set; }

    // Computed (populated by PipeRunsPhaseView.BtnComputeSlopes)
    public double SlopePercent   { get; set; }
    public double ComputedLength { get; set; }

    // Flow Logic
    public bool FlowLocked { get; set; }
    public bool FlowReversed { get; set; }

    public override string ToString() => $"Pipe {Id} ({FromPointId} -> {ToPointId}) D={Diameter}";
}
