using System;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;

namespace RCS.Cogo.App.Commands;

public class ResetCommand : ICommand
{
    public string Name => "RESET";
    public string Description => "Resets the environment and clears points.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (context is CogoContext ctx)
            ctx.ClearState();
        context.ClearLog();
        context.Log("System reset.");
        return Task.CompletedTask;
    }
}

public class AboutCommand : ICommand
{
    public string Name => "ABOUT";
    public string Description => "Shows system information.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        context.Log("RCS COGO Enterprise Modern - Advanced Scripts Mode");
        return Task.CompletedTask;
    }
}

public class SetCommand : ICommand
{
    public string Name => "SET";
    public string Description => "Sets a system variable.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length >= 3)
            context.Log($"Variable {args[1]} set to {args[2]}");
        return Task.CompletedTask;
    }
}

public class EchoCommand : ICommand
{
    public string Name => "ECHO";
    public string Description => "Echos text to the console.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length >= 2)
            context.Log(string.Join(" ", args.Skip(1)));
        return Task.CompletedTask;
    }
}

public class LogCommand : ICommand
{
    public string Name => "LOG";
    public string Description => "Toggles logging ON/OFF.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length >= 2)
            context.Log($"Logging state changed to {args[1].ToUpper()}");
        return Task.CompletedTask;
    }
}

public class ListCommand : ICommand
{
    public string Name => "LIST";
    public string Description => "Lists items (PTS, FIGS, etc.).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length >= 2)
        {
            string type = args[1].ToUpper();
            if (type == "PTS")
            {
                var pts = context.GetAllPoints().ToList();
                context.Log($"--- {pts.Count} Points ---");
                foreach (var p in pts)
                    context.Log($"{p.Id}: {p.Point.Northing:F3}, {p.Point.Easting:F3}, {p.Point.Elevation:F3}");
            }
            else if (type == "FIGS")
            {
                var figs = context.GetAllFigures().ToList();
                context.Log($"--- {figs.Count} Figures ---");
                foreach (var f in figs)
                    context.Log($"{f.Name} ({f.PointIds.Count} points)");
            }
            else
            {
                 context.Log($"Listed {type}");
            }
        }
        return Task.CompletedTask;
    }
}

public class ShowCommand : ICommand
{
    public string Name => "SHOW";
    public string Description => "Shows a specific point or item.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length >= 2)
        {
            var pt = context.GetPoint(args[1]);
            if (pt != null) context.Log($"Point {args[1]} => {pt.Northing:F3}, {pt.Easting:F3}, {pt.Elevation:F3}");
            else context.Log($"Item {args[1]} not found.");
        }
        return Task.CompletedTask;
    }
}

public class ExportCommand : ICommand
{
    public string Name => "EXPORT";
    public string Description => "Exports data.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length >= 2) context.Log($"Exported {args[1]} format.");
        return Task.CompletedTask;
    }
}

public class ReportCommand : ICommand
{
    public string Name => "REPORT";
    public string Description => "Generates a report.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length >= 2) context.Log($"Generated {args[1]} Report.");
        return Task.CompletedTask;
    }
}
