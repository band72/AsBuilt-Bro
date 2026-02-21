using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class DelCommand : ICommand
{
    public string Name => "DEL";
    public string Description => "Delete a point. Usage: DEL <Pt>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2) return Task.CompletedTask;
        
        string target = args[1].ToUpper();
        if (target == "PTS")
        {
            if (args.Length >= 4)
            {
                if (int.TryParse(args[2], out int startPt) && int.TryParse(args[3], out int endPt))
                {
                    int min = System.Math.Min(startPt, endPt);
                    int max = System.Math.Max(startPt, endPt);
                    int count = 0;
                    for (int i = min; i <= max; i++)
                    {
                        if (context.DeletePoint(i.ToString())) count++;
                    }
                    context.Log($"Deleted {count} points between {min} and {max}.");
                }
                else
                {
                    context.Log("Error: Invalid point range. Usage: DEL PTS <Start> <End>");
                }
            }
            else
            {
                var pts = context.GetAllPoints().Select(p => p.Id).ToList();
                foreach (var pt in pts) context.DeletePoint(pt);
                
                context.CurrentStation = null;
                context.CurrentBacksight = null;
                context.Log("All points deleted.");
            }
        }
        else if (target == "FIG" || target == "FIGS")
        {
            var figs = context.GetAllFigures().Select(f => f.Name).ToList();
            foreach (var fig in figs) context.DeleteFigure(fig);
            context.CurrentFigure = null;
            context.Log("All figures deleted.");
        }
        else
        {
            context.DeletePoint(args[1]);
            context.Log($"Point {args[1]} deleted.");
        }
        
        return Task.CompletedTask;
    }
}

public class HelpCommand : ICommand
{
    private readonly CommandRegistry _registry;
    public HelpCommand(CommandRegistry registry) => _registry = registry;
    
    public string Name => "HELP";
    public string Description => "List all commands.";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        var cmds = _registry.GetAllCommands().OrderBy(c => c.Name);
        var sb = new StringBuilder();
        sb.AppendLine("--- Available Commands ---");
        foreach(var c in cmds)
        {
            sb.AppendLine($"{c.Name}: {c.Description}");
        }
        context.Log(sb.ToString());
        return Task.CompletedTask;
    }
}

public class ClearCommand : ICommand
{
    public string Name => "CLEAR";
    public string Description => "Clear the output log.";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        context.ClearLog();
        return Task.CompletedTask;
    }
}
