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
        if (!context.AutoPoint && args.Length < 4)
        {
            context.Log("Error: Usage: NEZ <Pt> <N> <E> [Z] [Desc] or enable AP to omit <Pt>");
            return Task.CompletedTask;
        }

        string ptId;
        int startIdx;

        if (context.AutoPoint)
        {
            ptId = context.GetNextPointId().ToString();
            startIdx = 1;
        }
        else
        {
            ptId = args[1];
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
            else if (args[i].ToUpper() != "N" && args[i].ToUpper() != "E" && args[i].ToUpper() != "Z" && args[i].ToUpper() != "DESC" && args[i].ToUpper() != "DIR")
            {
                if (string.IsNullOrEmpty(desc)) desc = args[i].Trim('"');
                else desc += " " + args[i].Trim('"');
            }
        }

        if (numbers.Count < 2)
        {
            context.Log("Error: Invalid coordinates. Need N, E, [Z].");
            return Task.CompletedTask;
        }

        double z = numbers.Count >= 3 ? numbers[2] : 0.0;
        var pt = new Point3D(numbers[0], numbers[1], z);
        context.AddPoint(ptId, pt, desc); // AddPoint will overwrite/update if exists usually, or should we check?
        // Context.AddPoint implementation likely handles dictionary key set.
        
        context.Log($"Point {ptId} created: {pt}");

        return Task.CompletedTask;
    }
}
