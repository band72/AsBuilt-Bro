using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class OutputCommand : ICommand
{
    public string Name        => "OUTPUT";
    public string Description => "Toggle printed output to log (ON / OFF). When OFF, subsequent Log() calls are suppressed.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1)
        {
            bool enable = args[1].Trim().ToUpperInvariant() != "OFF";
            context.OutputEnabled = enable;
            context.Log($"Output: {(enable ? "ON" : "OFF")}");
        }
        else
        {
            context.Log($"Output is currently: {(context.OutputEnabled ? "ON" : "OFF")}");
        }
        return Task.CompletedTask;
    }
}

public class HistoryCommand : ICommand
{
    public string Name => "HISTORY";
    public string Description => "Toggle History (ON/OFF).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"History: {args[1].ToUpper()}");
         return Task.CompletedTask;
    }
}

public class DittoCommand : ICommand
{
    public string Name => "DITTO";
    public string Description => "Toggle Ditto (ON/OFF).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"Ditto: {args[1].ToUpper()}");
         return Task.CompletedTask;
    }
}

public class RedrawCommand : ICommand
{
    public string Name => "REDRAW";
    public string Description => "Redraws graphics.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // UI should auto-refresh, but we can log a signal if we had an event bus.
        context.Log("Graphics Redrawn.");
        return Task.CompletedTask;
    }
}

public class PanCommand : ICommand
{
    public string Name => "PAN";
    public string Description => "Pan graphics.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // Interactive or explicit?
        // Usually Interactive.
        context.Log("Use mouse to Pan graphics.");
        return Task.CompletedTask;
    }
}

public class ZoomCommand : ICommand
{
    public string Name => "ZOOM";
    public string Description => "Zoom commands (EXTENTS, WINDOW, etc).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1)
        {
            string sub = args[1].ToUpper();
            context.Log($"Zoom: {sub}");
            // In a real app, this would modify Viewport offsets/scale.
        }
        else
        {
            context.Log("Use mouse scroll to Zoom.");
        }
        return Task.CompletedTask;
    }
}

public class DispCommand : ICommand
{
    public string Name => "DISP";
    public string Description => "Display Info/Toggle.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.Log($"Display: {args[1]}");
        return Task.CompletedTask;
    }
}

public class SkipCommand : ICommand
{
    public string Name        => "SKIP";
    public string Description => "Skip N blank lines in printed output. Args: SKIP [n]";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        int n = (args.Length > 1 && int.TryParse(args[1], out var v)) ? v : 1;
        context.Log(string.Join(string.Empty, System.Linq.Enumerable.Repeat(Environment.NewLine, n)));
        return Task.CompletedTask;
    }
}

public class IdCommand : ICommand
{
    public string Name => "ID";
    public string Description => "Identify Point or Figure.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1)
         {
             var id = args[1];
             // Try point
             var pt = context.GetPoint(id);
             if (pt != null)
             {
                 context.Log($"Point {id}: {pt}");
                 return Task.CompletedTask;
             }
             // Try figure
             var fig = context.GetFigure(id);
             if (fig != null)
             {
                 context.Log($"Figure {id}: {fig.PointIds.Count} points.");
                 return Task.CompletedTask;
             }
             context.Log($"ID {id} not found.");
         }
         return Task.CompletedTask;
    }
}

public class SqCommand : ICommand
{
    public string Name => "SQ"; // Scroll/Query? Or Square? Or SideShot?
    // Often "Scroll Queue" or similar. Or "Select Query"
    // User script: "SQ set to 100".
    public string Description => "Set SQ.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.Log($"SQ set to {args[1]}");
        return Task.CompletedTask;
    }
}
