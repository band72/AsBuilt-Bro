using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class BbCommand : ICommand
{
    public string Name => "BB";
    public string Description => "Intersection by two Bearings. Usage: BB <PtNew> <Pt1> <Quad1> <Brg1> <Pt2> <Quad2> <Brg2> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 8)
        {
            context.Log("Error: Usage: BB <PtNew> <Pt1> <Q1> <B1> <Pt2> <Q2> <B2> [Desc]");
            return Task.CompletedTask;
        }

        string newPt = args[1];
        string id1 = args[2];
        string id2 = args[5];

        var p1 = context.GetPoint(id1);
        var p2 = context.GetPoint(id2);

        if (p1 == null || p2 == null)
        {
            context.Log("Error: One or both reference points not found.");
            return Task.CompletedTask;
        }

        // Parse 1
        if (!int.TryParse(args[3], out int q1) || !double.TryParse(args[4], out double b1))
        {
            context.Log("Error: Invalid Quadrant/Bearing 1.");
            return Task.CompletedTask;
        }

        // Parse 2
        if (!int.TryParse(args[6], out int q2) || !double.TryParse(args[7], out double b2))
        {
            context.Log("Error: Invalid Quadrant/Bearing 2.");
            return Task.CompletedTask;
        }

        string desc = args.Length > 8 ? args[8] : "";

        try
        {
            var az1 = Angle.FromQuadrant(q1, b1);
            var az2 = Angle.FromQuadrant(q2, b2);

            var result = GeometryEngine.IntersectionBearingBearing(p1, az1, p2, az2);

            if (result != null)
            {
                context.AddPoint(newPt, result, desc);
                context.Log($"Point {newPt} created at intersection: {result}");
            }
            else
            {
                context.Log("Error: Parallel lines, no intersection.");
            }
        }
        catch(System.Exception ex)
        {
            context.Log($"Error: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
