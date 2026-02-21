using System.IO;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class LoadCommand : ICommand
{
    private readonly CommandRegistry _registry;

    public LoadCommand(CommandRegistry registry)
    {
        _registry = registry;
    }

    public string Name => "LOAD";
    public string Description => "Loads and executes a .cogo script file. Usage: LOAD <Filename>";

    public async Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: Usage: LOAD <Filename>");
            return;
        }

        string filename = args[1];
        if (!File.Exists(filename) && File.Exists(filename + ".cogo"))
            filename += ".cogo";

        if (!File.Exists(filename))
        {
            context.Log($"Error: File {filename} not found.");
            return;
        }

        try
        {
            string[] lines = File.ReadAllLines(filename);
            context.Log($"Loading {filename} ({lines.Length} lines)...");
            
            // Create a temporary engine to execute lines
            // We reuse the same Context!
            var engine = new ScriptEngine(_registry);
            
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//"))
                    continue;

                await engine.ExecuteAsync(trimmed, context);
            }
            
            context.Log("Load complete.");
        }
        catch (System.Exception ex)
        {
            context.Log($"Error loading file: {ex.Message}");
        }
    }
}
