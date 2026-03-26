using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;

namespace RCS.Cogo.App.Commands;

/// <summary>
/// SEG — Segment break. Ends the current figure and starts a new segment
/// of the same base name (EP → EP_2, EP_3, …).
/// Usage: SEG
/// Equivalent to: END followed by BEG &lt;same-figure-name&gt;
/// </summary>
public class SegCommand : ICommand
{
    public string Name => "SEG";
    public string Description => "Breaks the current figure and starts a new segment. Usage: SEG";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (context.CurrentFigure == null)
        {
            context.Log("SEG: No active figure to break. Use BEG <name> first.");
            return Task.CompletedTask;
        }

        // Capture the base name before ending the current figure
        string baseName = context.CurrentFigure.Name;

        // Strip any existing _N suffix so we always branch from the base
        int underscoreIdx = baseName.LastIndexOf('_');
        if (underscoreIdx > 0 && int.TryParse(baseName.Substring(underscoreIdx + 1), out _))
            baseName = baseName.Substring(0, underscoreIdx);

        // End the current segment
        context.Log($"[SEG] Closing segment '{context.CurrentFigure.Name}' ({context.CurrentFigure.PointIds.Count} pts).");
        context.CurrentFigure = null;

        // Find the next free segment name: baseName, baseName_2, baseName_3, …
        string newName = baseName;
        int seg = 2;
        while (context.GetFigure(newName) != null)
        {
            newName = $"{baseName}_{seg}";
            seg++;
        }

        var newFig = new Figure(newName);
        context.AddFigure(newFig);
        context.CurrentFigure = newFig;
        context.Log($"[SEG] Started new segment '{newName}'.");

        return Task.CompletedTask;
    }
}
