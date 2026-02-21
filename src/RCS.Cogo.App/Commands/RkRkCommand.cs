using System;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class RkRkCommand : ICommand
{
    public string Name => "RKRK";
    public string Description => "Intersection Distance-Distance (Range-Known Range-Known). Usage: RKRK <P1> <Dist1> <P2> <Dist2>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 5)
        {
            context.Log("Usage: RKRK <P1> <Dist1> <P2> <Dist2>");
            return Task.CompletedTask;
        }

        string p1Id = args[1];
        string dist1Str = args[2];
        string p2Id = args[3];
        string dist2Str = args[4];

        var p1 = context.GetPoint(p1Id);
        var p2 = context.GetPoint(p2Id);

        if (p1 == null) 
        { 
            context.Log($"Error: Point '{p1Id}' not found."); 
            return Task.CompletedTask; 
        }
        if (p2 == null) 
        { 
            context.Log($"Error: Point '{p2Id}' not found."); 
            return Task.CompletedTask; 
        }

        if (!double.TryParse(dist1Str, out double r1)) 
        { 
            context.Log($"Error: Invalid Distance 1 '{dist1Str}'"); 
            return Task.CompletedTask; 
        }
        if (!double.TryParse(dist2Str, out double r2)) 
        { 
            context.Log($"Error: Invalid Distance 2 '{dist2Str}'"); 
            return Task.CompletedTask; 
        }

        try
        {
            var (left, right) = GeometryEngine.IntersectionDistanceDistance(p1, r1, p2, r2);

            if (left == null || right == null)
            {
                context.Log("Error: No intersection found (Circles do not meet, are contained, or concentric).");
                return Task.CompletedTask;
            }
            
            context.LastIntersections = (left, right);
            context.Log($"Found two intersections.");
            context.Log($"L: N={left.Northing:F4} E={left.Easting:F4}");
            context.Log($"R: N={right.Northing:F4} E={right.Easting:F4}");
            context.Log("Use 'SAVE <NORTH|SOUTH|EAST|WEST> <PtNew>' to select and store a point.");

        }
        catch (Exception ex)
        {
            context.Log($"Error executing RKRK: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
