using System;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class RkRkCommand : ICommand
{
    public string Name => "RKRK";
    public string Description => "Intersection Distance-Distance (Range-Known Range-Known). Usage: RKRK <PtNew> <P1> <Dist1> <P2> <Dist2>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 6)
        {
            context.Log("Usage: RKRK <PtNew> <P1> <Dist1> <P2> <Dist2>");
            return Task.CompletedTask;
        }

        string newPt = args[1];
        string p1Id = args[2];
        string dist1Str = args[3];
        string p2Id = args[4];
        string dist2Str = args[5];

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
            // Calculate Intersections
            // Returns (Left, Right) relative to P1->P2 vector.
            var (left, right) = GeometryEngine.IntersectionDistanceDistance(p1, r1, p2, r2);

            if (left == null || right == null)
            {
                context.Log("Error: No intersection found (Circles do not meet, are contained, or concentric).");
                return Task.CompletedTask;
            }
            
            // User requirement: "save the point to the database and not the file."
            // We create both solutions usually, or ask.
            // Given singular "the point", likely expects the Right solution as primary.
            // We will create both: <Pt> (Right) and <Pt>_L (Left).
            
            // Right Solution (Primary)
            context.AddPoint(newPt, right, "RKRK Intersection (Right)");
            context.Log($"Point {newPt} created at N:{right.Northing:F4}, E:{right.Easting:F4} (Right Sol)");
            
            // Left Solution (Alternate)
            string leftId = newPt + "_L";
            context.AddPoint(leftId, left, "RKRK Intersection (Left)");
            context.Log($"Point {leftId} created at N:{left.Northing:F4}, E:{left.Easting:F4} (Left Sol)");

        }
        catch (Exception ex)
        {
            context.Log($"Error executing RKRK: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
