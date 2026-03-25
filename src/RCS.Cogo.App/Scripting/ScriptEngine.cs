using System;
using System.Collections.Generic;
using System.Linq;
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

        // Strip inline comments before anything else.
        // Handles: PT 1 100 200  // this is a comment
        //          PT 1 100 200  ; or semicolon comment
        commandLine = StripInlineComment(commandLine);

        if (string.IsNullOrWhiteSpace(commandLine)) return;

        string trimmed = commandLine.TrimStart();

        // Skip whole-line comments
        if (trimmed.StartsWith("!"))  return;
        if (trimmed.StartsWith("//")) return;
        if (trimmed.StartsWith("/"))  return;
        if (trimmed.StartsWith(";"))  return;
        if (trimmed.StartsWith("--")) return;   // SQL-style comment
        if (trimmed.StartsWith("#"))  return;   // Python/hash comment

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
            commandName.Equals("pipe-engine-on",  StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_cogoEngineOff) return;

        try
        {
            var command = _registry.GetCommand(commandName);
            if (command != null)
            {
                await command.ExecuteAsync(args.ToArray(), context);
            }
            else
            {
                // Numeric-first line → implicit PT or CONT
                if (double.TryParse(commandName, out _))
                {
                    if (args.Count >= 3)
                    {
                        var ptCmd = _registry.GetCommand("PT");
                        if (ptCmd != null)
                        {
                            var newArgs = new List<string> { "PT" };
                            newArgs.AddRange(args);
                            await ptCmd.ExecuteAsync(newArgs.ToArray(), context);
                            return;
                        }
                    }
                    else
                    {
                        var contCmd = _registry.GetCommand("CONT");
                        if (contCmd != null)
                        {
                            var newArgs = new List<string> { "CONT" };
                            newArgs.AddRange(args);
                            await contCmd.ExecuteAsync(newArgs.ToArray(), context);
                            return;
                        }
                    }
                }

                // Only log "Unknown command" for tokens that look like commands (not stray
                // punctuation, unicode chars, or leftover fragments from comment stripping).
                if (commandName.Length >= 2 && commandName.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                    context.Log($"Unknown command: {commandName}");
            }
        }
        catch (Exception ex)
        {
            context.Log($"[ERROR] Command failed: {ex.Message}");
        }
    }

    // ── Inline comment stripper ─────────────────────────────────────────────
    // Removes everything from the first unquoted // or ; to end of line.
    private static string StripInlineComment(string line)
    {
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (inQuotes) continue;

            // // comment
            if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
                return line[..i].TrimEnd();

            // ; comment (but not if it's the first non-whitespace — that's handled above)
            if (c == ';')
                return line[..i].TrimEnd();
        }
        return line;
    }

    // ── Tokenizer ───────────────────────────────────────────────────────────
    // Splits on whitespace, respects double-quoted strings. 
    // Uppercases the first token (command name) automatically.
    private List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        var sb     = new StringBuilder();
        bool inQuotes = false;
        bool firstToken = true;

        foreach (char c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (sb.Length > 0)
                {
                    string token = sb.ToString();
                    tokens.Add(firstToken ? token.ToUpperInvariant() : token);
                    firstToken = false;
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
            string token = sb.ToString();
            tokens.Add(firstToken ? token.ToUpperInvariant() : token);
        }

        return tokens;
    }
}
