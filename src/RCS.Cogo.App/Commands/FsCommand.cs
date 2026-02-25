using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class FsCommand : ICommand
{
    public string Name => "FS";
    public string Description => "Creates a foresight point by Angle from Backsight. Usage: FS <Point> <Angle> <Dist> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // Executes exactly like an Angle Right/Distance (AD) command but guarantees instrument DOES NOT move.
        bool oldTraverse = context.TraverseMode;
        context.TraverseMode = false;
        
        var cmd = new AdCommand();
        var task = cmd.ExecuteAsync(args, context);

        context.TraverseMode = oldTraverse;
        return task;
    }
}
