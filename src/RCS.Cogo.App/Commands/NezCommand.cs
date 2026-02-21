using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class NezCommand : ICommand
{
    public string Name => "NEZ";
    public string Description => "Create point with Northing, Easting, and Elevation. Usage: NEZ <Pt> <N> <E> <Z> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 5)
        {
            context.Log("Error: Usage: NEZ <Pt> <N> <E> <Z> [Desc]");
            return Task.CompletedTask;
        }

        string ptId = args[1];

        if (!double.TryParse(args[2], out double n) || 
            !double.TryParse(args[3], out double e) ||
            !double.TryParse(args[4], out double z))
        {
            context.Log("Error: Invalid coordinates.");
            return Task.CompletedTask;
        }

        string desc = args.Length > 5 ? args[5] : "";

        var pt = new Point3D(n, e, z);
        context.AddPoint(ptId, pt, desc); // AddPoint will overwrite/update if exists usually, or should we check?
        // Context.AddPoint implementation likely handles dictionary key set.
        
        context.Log($"Point {ptId} created: {pt}");

        return Task.CompletedTask;
    }
}
