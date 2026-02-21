using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCS.Cogo.App.Scripting;

public class ScriptEngine
{
    private readonly CommandRegistry _registry;
    private bool _cogoEngineOff = false;

    public ScriptEngine(CommandRegistry registry)
    {
        _registry = registry;
    }

    public async Task ExecuteAsync(string commandLine, ICogoContext context)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return;
        
        // Ignore comments
        if (commandLine.TrimStart().StartsWith("!")) return;
        if (commandLine.TrimStart().StartsWith("//")) return;
        if (commandLine.TrimStart().StartsWith("/")) return;
        if (commandLine.TrimStart().StartsWith(";")) return;

        var args = Tokenize(commandLine);
        if (args.Count == 0) return;

        string commandName = args[0];

        if (commandName.Equals("cogo-engine-off", StringComparison.OrdinalIgnoreCase))
        {
            _cogoEngineOff = true;
            context.Log("COGO Engine script processing PAUSED.");
            return;
        }

        if (commandName.Equals("cogo-engine-on", StringComparison.OrdinalIgnoreCase))
        {
            _cogoEngineOff = false;
            context.Log("COGO Engine script processing RESUMED.");
            return;
        }

        if (commandName.Equals("pipe-engine-off", StringComparison.OrdinalIgnoreCase) ||
            commandName.Equals("pipe-engine-on", StringComparison.OrdinalIgnoreCase))
        {
            // Ignored cleanly by COGO engine
            return;
        }

        if (_cogoEngineOff) return;
        
        var command = _registry.GetCommand(commandName);
        if (command != null)
        {
            // Pass all args including command name, or just the rest?
            // Usually simpler to pass all so command knows its own name if aliased.
            await command.ExecuteAsync(args.ToArray(), context);
        }
        else
        {
            context.Log($"Unknown command: {commandName}");
        }
    }

    private List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                // Don't add the quote char itself if we want clean strings
                // But sometimes we want to keep them. Let's strip them for now.
                continue; 
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (sb.Length > 0)
                {
                    tokens.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else
            {
                sb.Append(c);
            }
        }

        if (sb.Length > 0)
        {
            tokens.Add(sb.ToString());
        }

        return tokens;
    }
}
