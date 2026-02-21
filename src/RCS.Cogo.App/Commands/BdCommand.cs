using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class BdCommand : ICommand
{
    public string Name => "BD";
    public string Description => "Create point by Bearing (Quadrant) and Distance from Station. Usage: BD <Pt> <Quad> <Brg> <Dist> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (context.CurrentStation == null)
        {
            context.Log("Error: No station set. Use STN command.");
            return Task.CompletedTask;
        }

        if (args.Length < 5)
        {
            context.Log("Usage: BD <Pt> <Quad> <Brg> <Dist> [Desc] (Can swap Quad/Brg)");
            return Task.CompletedTask;
        }

        string ptId = args[1];
        
        // Try parsing args 2 and 3 as doubles
        if (!double.TryParse(args[2], out double val1))
        {
            context.Log($"Error: Invalid number '{args[2]}'.");
            return Task.CompletedTask;
        }
        if (!double.TryParse(args[3], out double val2))
        {
            context.Log($"Error: Invalid number '{args[3]}'.");
            return Task.CompletedTask;
        }

        int quad;
        double bearingDms;

        // Auto-detect Order:
        // Quadrant must be integer 1-4.
        // Bearing is usually < 90, but can be anything technically.
        // Heuristic: If val1 > 4 and val2 is integer 1-4, assume reversed (Bearing Quad).
        bool val1IsQuad = (val1 >= 1 && val1 <= 4 && val1 % 1 == 0);
        bool val2IsQuad = (val2 >= 1 && val2 <= 4 && val2 % 1 == 0);

        if (val1IsQuad && !val2IsQuad)
        {
            // Standard: Quad Bearing
            quad = (int)val1;
            bearingDms = val2;
        }
        else if (!val1IsQuad && val2IsQuad)
        {
            // Reversed: Bearing Quad
            bearingDms = val1;
            quad = (int)val2;
            context.Log("(Interpreted as Bearing then Quadrant)");
        }
        else if (val1IsQuad && val2IsQuad)
        {
            // Ambiguous (e.g. 1 2). Assume Standard.
            quad = (int)val1;
            bearingDms = val2;
        }
        else
        {
            // Neither is a valid quadrant? 
            // Or both > 4 ?
            // Fallback to Standard logic which will error on Quad.
            quad = (int)val1; 
            bearingDms = val2;
        }

        if (quad < 1 || quad > 4)
        {
            context.Log($"Error: Invalid Quadrant {quad} (Must be 1-4).");
            return Task.CompletedTask;
        }
        
        // Arg 4 is Distance
        if (!double.TryParse(args[4], out double dist))
        {
            context.Log("Error: Invalid Distance.");
            return Task.CompletedTask;
        }

        string desc = args.Length > 5 ? args[5] : "";

        try
        {
            var azimuth = Angle.FromQuadrant(quad, bearingDms);
            var newPoint = GeometryEngine.Forward(context.CurrentStation, azimuth, dist);
            context.AddPoint(ptId, newPoint, desc);
            context.Log($"Point {ptId} created at {newPoint}");
        }
        catch (System.Exception ex)
        {
             context.Log($"Error: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
