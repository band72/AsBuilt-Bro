using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class NeCommand : ICommand
{
    public string Name => "NE";
    public string Description => "Creates a point by Northing/Easting. Usage: NE <PointId> <North> <East> [Description]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // NE 1 5000 5000 "Start"
        if (args.Length < 4)
        {
            context.Log("Error: Invalid arguments. Usage: NE <Point> <N> <E> [Desc]");
            return Task.CompletedTask;
        }

        string pointId = args[1];
        if (!double.TryParse(args[2], out double n) || !double.TryParse(args[3], out double e))
        {
            context.Log("Error: Invalid coordinates.");
            return Task.CompletedTask;
        }

        string desc = args.Length > 4 ? args[4] : "";
        
        var pt = new Point3D(n, e, 0); // NE implies Elevation 0 or unchanged? Let's use 0.
        context.AddPoint(pointId, pt, desc);
        context.Log($"Point {pointId} created at {pt}");
        
        // Auto-set station if none exists?
        if (context.CurrentStation == null)
        {
            context.CurrentStation = pt;
            context.Log($"Auto-set Station to {pointId}");
        }

        return Task.CompletedTask;
    }
}
