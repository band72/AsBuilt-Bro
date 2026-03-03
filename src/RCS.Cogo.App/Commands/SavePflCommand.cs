using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class SavePflCommand : ICommand
{
    public string Name => "SAVE-PFL";
    public string Description => "Saves the script to the Profile Alignment table. Usage: SAVE-PFL <Name> [Description]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: Usage: SAVE-PFL <Name> [Description]");
            return Task.CompletedTask;
        }

        string name = args[1];
        if (name.StartsWith("\"") && name.EndsWith("\"") && name.Length >= 2) name = name.Substring(1, name.Length - 2);

        string desc = string.Join(" ", args.Skip(2));
        if (desc.StartsWith("\"") && desc.EndsWith("\"") && desc.Length >= 2) desc = desc.Substring(1, desc.Length - 2);

        if (context.SaveProfileAlignmentAction != null)
        {
            context.SaveProfileAlignmentAction(name, desc);
        }
        else
        {
            context.Log("Error: SaveProfileAlignmentAction not wired in current context.");
        }

        return Task.CompletedTask;
    }
}
