using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class InvCommand : ICommand
{
    public string Name => "INV";
    public string Description => "Calculates Inverse between two points. Usage: INV <Pt1> <Pt2>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // INV 1 2
        if (args.Length < 3)
        {
            context.Log("Error: Usage: INV <Pt1> <Pt2>");
            return Task.CompletedTask;
        }

        string p1Id = args[1];
        string p2Id = args[2];

        var p1 = context.GetPoint(p1Id);
        var p2 = context.GetPoint(p2Id);

        if (p1 == null || p2 == null)
        {
            context.Log("Error: One or both points not found.");
            return Task.CompletedTask;
        }

        var result = GeometryEngine.Inverse(p1, p2);
        
        context.Log($"Inverse {p1Id}-{p2Id}:");
        context.Log($"  Azimuth:  {result.Azimuth.ToDMS():F4}");
        context.Log($"  Distance: {result.Distance:F3}");
        
        return Task.CompletedTask;
    }
}
