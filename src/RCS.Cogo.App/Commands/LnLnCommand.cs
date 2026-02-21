using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class LnLnCommand : ICommand
{
    public string Name => "LNLN";
    public string Description => "Intersection of two lines with offsets. Usage: LNLN <PtNew> <P1> <P2> <Off1> <P3> <P4> <Off2> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // Flexible parsing: LNLN <PtNew> <P1> <P2> [Off1] <P3> <P4> [Off2] [Desc]
        
        if (args.Length < 6)
        {
            context.Log("Error: Usage: LNLN <PtNew> <P1> <P2> [Off1] <P3> <P4> [Off2] [Desc]");
            return Task.CompletedTask;
        }

        string ptId = args[1];
        string p1Id = args[2];
        string p2Id = args[3];
        
        int currentIndex = 4;
        double off1 = 0.0;
        
        // Try parsing args[4] as double. If success, it's offset. Else it's P3.
        if (double.TryParse(args[currentIndex], out double val))
        {
            off1 = val;
            currentIndex++;
        }
        
        if (currentIndex + 1 >= args.Length)
        {
             context.Log("Error: Missing second line points.");
             return Task.CompletedTask;
        }

        string p3Id = args[currentIndex];
        string p4Id = args[currentIndex + 1];
        currentIndex += 2;
        
        double off2 = 0.0;
        if (currentIndex < args.Length && double.TryParse(args[currentIndex], out double val2))
        {
            off2 = val2;
            currentIndex++;
        }
        
        string desc = "";
        if (currentIndex < args.Length)
        {
            desc = args[currentIndex];
        }

        var p1 = context.GetPoint(p1Id);
        var p2 = context.GetPoint(p2Id);
        var p3 = context.GetPoint(p3Id);
        var p4 = context.GetPoint(p4Id);

        if (p1 == null || p2 == null || p3 == null || p4 == null)
        {
            context.Log("Error: One or both reference points not found.");
            return Task.CompletedTask;
        }

        var result = GeometryEngine.IntersectionLineLine(p1, p2, off1, p3, p4, off2);

        if (result != null)
        {
            context.AddPoint(ptId, result, desc);
            context.Log($"Point {ptId} created at intersection: {result}");
        }
        else
        {
            context.Log("Error: Parallel lines, no intersection.");
        }

        return Task.CompletedTask;
    }
}
