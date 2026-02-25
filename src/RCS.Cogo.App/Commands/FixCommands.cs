using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class TravCommand : ICommand
{
    public string Name => "TRAV";
    public string Description => "Toggle Traverse Mode or create Traverse Point. Usage: TRAV [ON|OFF] or TRAV <Pt> <Ang> <Dist>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1)
        {
            string mode = args[1].ToUpper();
            if (mode == "ON")
            {
                context.TraverseMode = true;
                context.Log("Traverse Mode ON");
            }
            else if (mode == "OFF")
            {
                context.TraverseMode = false;
                context.Log("Traverse Mode OFF");
            }
            else
            {
                // Must be creating a traverse point: TRAV 102 134.2029 117.37
                bool oldTraverse = context.TraverseMode;
                context.TraverseMode = true;
                
                var cmd = new AdCommand();
                var task = cmd.ExecuteAsync(args, context);

                context.TraverseMode = oldTraverse;
                return task;
            }
        }
        else
        {
            context.Log($"Traverse Mode is {(context.TraverseMode ? "ON" : "OFF")}");
        }
        return Task.CompletedTask;
    }
}

public class PntCommand : ICommand
{
    public string Name => "PNT"; // Alias for POINT or CONT
    public string Description => "Add point to current figure. Usage: PNT <Pt>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // This acts exactly like CONT
        return new ContCommand().ExecuteAsync(args, context);
    }
}

public class MapChkCommand : ICommand
{
    public string Name => "MAPCHK"; // Alias for MAPCHECK
    public string Description => "Map Check Figure. Usage: MAPCHK <Figure>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        return new MapCheckCommand().ExecuteAsync(args, context);
    }
}

public class LCommand : ICommand
{
    public string Name => "L"; // Alias for CONT
    public string Description => "Add point to current figure (Line to). Usage: L <Pt>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        return new ContCommand().ExecuteAsync(args, context);
    }
}

public class CCommand : ICommand
{
    public string Name => "C"; // Alias for CLOSE
    public string Description => "Close current figure. Usage: C";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        return new CloseCommand().ExecuteAsync(args, context);
    }
}
