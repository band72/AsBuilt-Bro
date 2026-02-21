using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class AdCommand : ICommand
{
    public string Name => "AD";
    public string Description => "Creates a point by Angle Right from Backsight. Usage: AD <Point> <Angle> <Dist> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // AD 2 90.0000 100.00
        if (args.Length < 4)
        {
            context.Log("Error: Usage: AD <Point> <Angle> <Dist> [Desc]");
            return Task.CompletedTask;
        }

        if (context.CurrentStation == null)
        {
            context.Log("Error: No station set.");
            return Task.CompletedTask;
        }

        if (context.CurrentBacksight == null)
        {
            // If no backsight, maybe assume Azimuth 0? Or error?
            // Usually error.
            // But maybe user just wants to turn angle from North? That's ZD.
            context.Log("Error: No backsight set. Use BS command.");
            return Task.CompletedTask;
        }

        string pointId = args[1];
        if (!double.TryParse(args[2], out double angleDms) || !double.TryParse(args[3], out double dist))
        {
            context.Log("Error: Invalid number format.");
            return Task.CompletedTask;
        }
        
        string desc = args.Length > 4 ? args[4] : "";

        // Calculate Backsight Azimuth
        var bsAz = GeometryEngine.Inverse(context.CurrentStation, context.CurrentBacksight).Azimuth;
        
        // Add Angle Right
        var angleRight = Angle.FromDMS(angleDms);
        var az = bsAz + angleRight;
        
        // Normalize 0-360 handled by Angle? Angle wraps?
        // My Angle struct wraps only in ToDMS? No, Radians are double.
        // GeometryEngine.Forward uses cos/sin so larger angles are fine.
        
        var newPoint = GeometryEngine.Forward(context.CurrentStation, az, dist);
        
        context.AddPoint(pointId, newPoint, desc);
        context.Log($"Point {pointId} created at {newPoint} (Turned: {angleRight.ToDMS():F4}, Dist: {dist:F3})");

        return Task.CompletedTask;
    }
}
