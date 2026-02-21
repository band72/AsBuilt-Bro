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
        if (!context.AutoPoint && args.Length < 4)
        {
            context.Log("Error: Invalid arguments. Usage: NE <Point> <N> <E> [Desc] or enable AP to omit <Point>");
            return Task.CompletedTask;
        }

        string pointId;
        int startIdx;

        if (context.AutoPoint)
        {
            pointId = context.GetNextPointId().ToString();
            startIdx = 1;
        }
        else
        {
            pointId = args[1];
            startIdx = 2;
        }
        
        var numbers = new System.Collections.Generic.List<double>();
        string desc = "";
        
        for (int i = startIdx; i < args.Length; i++)
        {
            if (double.TryParse(args[i], out double val))
            {
                numbers.Add(val);
            }
            else if (args[i].ToUpper() != "N" && args[i].ToUpper() != "E" && args[i].ToUpper() != "DESC" && args[i].ToUpper() != "DIR")
            {
                if (string.IsNullOrEmpty(desc)) desc = args[i].Trim('"');
                else desc += " " + args[i].Trim('"');
            }
        }

        if (numbers.Count < 2)
        {
            context.Log("Error: Invalid coordinates. Need N, E.");
            return Task.CompletedTask;
        }

        var pt = new Point3D(numbers[0], numbers[1], 0); // NE implies Elevation 0
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
