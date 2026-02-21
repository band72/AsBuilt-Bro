using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class ZdCommand : ICommand
{
    public string Name => "ZD";
    public string Description => "Creates a point from Station by Azimuth and Distance. Usage: ZD <Point> <Azimuth> <Distance> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // ZD 2 45.0000 100.00 "Iron Pin"
        if (args.Length < 4)
        {
            context.Log("Error: Invalid arguments. Usage: ZD <Point> <Az> <Dist> [Desc]");
            return Task.CompletedTask;
        }

        if (context.CurrentStation == null)
        {
            context.Log("Error: No station set. Use STN or NE to set station.");
            return Task.CompletedTask;
        }

        string pointId = args[1];
        if (!double.TryParse(args[2], out double azDms) || !double.TryParse(args[3], out double dist))
        {
            context.Log("Error: Invalid number format.");
            return Task.CompletedTask;
        }

        string desc = args.Length > 4 ? args[4] : "";

        var azimuth = Angle.FromDMS(azDms);
        var newPoint = GeometryEngine.Forward(context.CurrentStation, azimuth, dist);
        
        // ZD creates 2D or 3D? Standard is 2D unless Vertical info provided.
        // Legacy cmdhelp: ZD [VA] ... for vertical.
        // We will stick to 2D computed coordinates, preserving Station Elevation?
        // Actually GeometryEngine.Forward keeps Z of start point.
        
        context.AddPoint(pointId, newPoint, desc);
        context.Log($"Point {pointId} created at {newPoint} (Az: {azimuth.ToDMS():F4}, Dist: {dist:F3})");

        if (context.TraverseMode)
        {
            context.CurrentBacksight = context.CurrentStation;
            context.CurrentStation = newPoint;
            context.Log($"Traversed to new station: {pointId}");
        }

        return Task.CompletedTask;
    }
}
