using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class SaveHalnCommand : ICommand
{
    public string Name => "SAVE-HALN";
    public string Description => "Saves the script to the Horizontal Alignment table. Usage: SAVE-HALN <Name> [Description]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: Usage: SAVE-HALN <Name> [Description]");
            return Task.CompletedTask;
        }

        string name = args[1];
        // Strip quotes if they provided any
        if (name.StartsWith("\"") && name.EndsWith("\"") && name.Length >= 2) name = name.Substring(1, name.Length - 2);

        string desc = string.Join(" ", args.Skip(2));
        if (desc.StartsWith("\"") && desc.EndsWith("\"") && desc.Length >= 2) desc = desc.Substring(1, desc.Length - 2);

        if (context.SaveHorizontalAlignmentAction != null)
        {
            context.SaveHorizontalAlignmentAction(name, desc);
        }
        else
        {
            context.Log("Error: SaveHorizontalAlignmentAction not wired in current context.");
        }

        return Task.CompletedTask;
    }
}
