using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class DelCommand : ICommand
{
    public string Name => "DEL";
    public string Description => "Delete a point. Usage: DEL <Pt>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2) return Task.CompletedTask;
        
        context.DeletePoint(args[1]);
        context.Log($"Point {args[1]} deleted.");
        
        return Task.CompletedTask;
    }
}

public class HelpCommand : ICommand
{
    private readonly CommandRegistry _registry;
    public HelpCommand(CommandRegistry registry) => _registry = registry;
    
    public string Name => "HELP";
    public string Description => "List all commands.";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        var cmds = _registry.GetAllCommands().OrderBy(c => c.Name);
        var sb = new StringBuilder();
        sb.AppendLine("--- Available Commands ---");
        foreach(var c in cmds)
        {
            sb.AppendLine($"{c.Name}: {c.Description}");
        }
        context.Log(sb.ToString());
        return Task.CompletedTask;
    }
}

public class ClearCommand : ICommand
{
    public string Name => "CLEAR";
    public string Description => "Clear the output log.";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        context.ClearLog();
        return Task.CompletedTask;
    }
}
