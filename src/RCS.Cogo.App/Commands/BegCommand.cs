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
            context.Log("Error: Usage: BEG <FigureName> or BEG FIG <FigureName>");
            return Task.CompletedTask;
        }

        string figName = args[1];
        if (args.Length >= 3 && figName.Equals("FIG", System.StringComparison.OrdinalIgnoreCase))
        {
            figName = args[2];
        }
        
        // ── If a figure with this name already exists, start a NEW segment ──
        // Appending to the same figure stitches unrelated point sequences into
        // one polyline and creates "crosslinks" (e.g. pt 204 → pt 2001).
        // Instead, find the next free name: EP, EP_2, EP_3, ...
        string baseName = figName;
        if (context.GetFigure(figName) != null)
        {
            int seg = 2;
            while (context.GetFigure($"{baseName}_{seg}") != null)
                seg++;
            figName = $"{baseName}_{seg}";
            context.Log($"[BEG] '{baseName}' already exists → starting new segment '{figName}'.");
        }

        var fig = new Figure(figName);
        context.AddFigure(fig);
        context.CurrentFigure = fig;
        context.Log($"Figure '{figName}' started.");

        return Task.CompletedTask;
    }
}
