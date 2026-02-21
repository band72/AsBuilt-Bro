using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class EndCommand : ICommand
{
    public string Name => "END";
    public string Description => "Ends the current Figure.";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (context.CurrentFigure != null)
        {
            context.Log($"Figure {context.CurrentFigure.Name} ended.");
            context.CurrentFigure = null;
        }
        else
        {
            context.Log("No active figure to end.");
        }

        return Task.CompletedTask;
    }
}
