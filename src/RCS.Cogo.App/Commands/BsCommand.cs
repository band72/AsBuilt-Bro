using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class BsCommand : ICommand
{
    public string Name => "BS";
    public string Description => "Sets the Backsight point. Usage: BS <Point>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: Usage: BS <Point>");
            return Task.CompletedTask;
        }

        string pointId = args[1];
        var pt = context.GetPoint(pointId);

        if (pt == null)
        {
            context.Log($"Error: Point {pointId} not found.");
            return Task.CompletedTask;
        }

        if (context.CurrentStation == null)
        {
            context.Log("Error: Station not set. Cannot set Backsight.");
            return Task.CompletedTask;
        }

        context.CurrentBacksight = pt;
        
        var az = GeometryEngine.Inverse(context.CurrentStation, pt).Azimuth;
        context.Log($"Backsight set to {pointId} (Azimuth: {az.ToDMS():F4})");
        
        return Task.CompletedTask;
    }
}
