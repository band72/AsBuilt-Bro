using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class StnCommand : ICommand
{
    public string Name => "STN";
    public string Description => "Sets the current occupied station. Usage: STN <PointId>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: Missing Point ID");
            return Task.CompletedTask;
        }

        string pointId = args[1];

        // If coordinates provided: STN PT N E Z DESC
        if (args.Length >= 5)
        {
            if (double.TryParse(args[2], out double n) &&
                double.TryParse(args[3], out double e) &&
                double.TryParse(args[4], out double z))
            {
                string desc = args.Length > 5 ? args[5].Trim('"') : "";
                
                // Create or Update point
                var newPt = new Point3D(n, e, z);
                context.AddPoint(pointId, newPt, desc); // Correct method signature
                
                context.CurrentStation = newPt;
                context.Log($"Stored and Occupied Point {pointId}: N:{n:F3} E:{e:F3} Z:{z:F3} D:{desc}");
                return Task.CompletedTask;
            }
            else
            {
                 context.Log("Error: Invalid coordinates. Usage: STN <PointId> <N> <E> <Z> [Desc]");
                 return Task.CompletedTask;
            }
        }

        // Existing lookup logic
        var pt = context.GetPoint(pointId);
        
        if (pt != null)
        {
            context.CurrentStation = pt;
            context.Log($"Station set to Point {pointId} at N:{pt.Northing:F3}, E:{pt.Easting:F3}, Z:{pt.Elevation:F3}");
        }
        else
        {
            context.Log($"Error: Point {pointId} not found. Create it first using NE command or provide coordinates: STN {pointId} N E Z \"Desc\"");
        }
        
        return Task.CompletedTask;
    }
}
