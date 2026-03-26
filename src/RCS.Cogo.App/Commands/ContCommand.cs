using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class ContCommand : ICommand
{
    public string Name => "CONT";
    public string Description => "Adds a point to the current Figure. Usage: CONT <Point>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (context.CurrentFigure == null)
        {
            context.Log("Error: No active figure. Use BEG <Name> to start a figure.");
            return Task.CompletedTask;
        }

        if (args.Length < 2)
        {
            context.Log("Error: Usage: CONT <Point>");
            return Task.CompletedTask;
        }

        string pointId = args[1];
        
        // Add point to figure. If the point hasn't been computed yet, log a warning
        // but still register it — the renderer null-checks each point at draw time.
        var pt = context.GetPoint(pointId);
        if (pt == null)
            context.Log($"[WARN] CONT: Point {pointId} not yet computed — will resolve when point is shot.");

        context.CurrentFigure.PointIds.Add(pointId);
        context.Log($"Point {pointId} added to Figure {context.CurrentFigure.Name}.");

        return Task.CompletedTask;
    }
}
