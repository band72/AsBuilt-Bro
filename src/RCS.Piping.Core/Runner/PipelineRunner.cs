using RCS.Piping.Core.Abstractions;
using RCS.Piping.Core.Models;

namespace RCS.Piping.Core.Runner;

public class PipelineRunner
{
    private readonly IPointProvider _points;
    private readonly PipeNetwork _network;

    public PipelineRunner(IPointProvider points, PipeNetwork network)
    {
        _points = points ?? throw new ArgumentNullException(nameof(points));
        _network = network ?? throw new ArgumentNullException(nameof(network));
    }

    public List<string> ValidateNetwork(IEnumerable<string>? validStructureTypes = null, IEnumerable<string>? validPipeTypes = null)
    {
        var issues = new List<string>();
        var validStructs = validStructureTypes?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var validPipes = validPipeTypes?.ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check Pipe Runs
        foreach (var run in _network.GetAllRuns())
        {
            if (!_points.PointExists(run.FromPointId))
                issues.Add($"Pipe {run.Id}: Start point '{run.FromPointId}' missing.");
            if (!_points.PointExists(run.ToPointId))
                issues.Add($"Pipe {run.Id}: End point '{run.ToPointId}' missing.");
                
            if (run.Diameter <= 0)
                issues.Add($"Pipe {run.Id}: Invalid diameter {run.Diameter}.");

            if (validPipes != null && !string.IsNullOrEmpty(run.Type) && !validPipes.Contains(run.Type))
                issues.Add($"Pipe {run.Id}: Invalid Type '{run.Type}'.");
        }

        // Check Structures
        foreach (var str in _network.GetAllStructures())
        {
            if (!_points.PointExists(str.PointId))
                issues.Add($"Structure {str.Id}: Location point '{str.PointId}' missing.");

            if (validStructs != null && !string.IsNullOrEmpty(str.Type) && !validStructs.Contains(str.Type))
                issues.Add($"Structure {str.Id}: Invalid Type '{str.Type}'.");
        }

        return issues;
    }

    public void CalculateSlopes()
    {
        foreach (var run in _network.GetAllRuns())
        {
            var p1 = _points.GetPoint(run.FromPointId);
            var p2 = _points.GetPoint(run.ToPointId);
            
            if (p1 != null && p2 != null)
            {
                // Calculate geometric length + slope if needed
                // Logic depends on Point3D props
            }
        }
    }
}
