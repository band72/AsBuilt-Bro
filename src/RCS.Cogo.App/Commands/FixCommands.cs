using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class TravCommand : ICommand
{
    public string Name => "TRAV";
    public string Description => "Toggle Traverse Mode. Usage: TRAV [ON|OFF]";

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
        }
        else
        {
            // Toggle or Display? Assuming Display if no arg
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
