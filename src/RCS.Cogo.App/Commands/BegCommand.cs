using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;

namespace RCS.Cogo.App.Commands;

public class BegCommand : ICommand
{
    public string Name => "BEG";
    public string Description => "Begins a new Figure. Usage: BEG <FigureName>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: Usage: BEG <FigureName>");
            return Task.CompletedTask;
        }

        string figName = args[1];
        
        // Check if exists? Overwrite?
        if (context.GetFigure(figName) != null)
        {
            context.Log($"Warning: Figure {figName} already exists. Appending to it.");
            context.CurrentFigure = context.GetFigure(figName);
        }
        else
        {
            var fig = new Figure(figName);
            context.AddFigure(fig);
            context.CurrentFigure = fig;
            context.Log($"Figure {figName} started.");
        }

        return Task.CompletedTask;
    }
}
