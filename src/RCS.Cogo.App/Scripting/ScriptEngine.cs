using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCS.Cogo.App.Scripting;

// ── Structured batch error entry ─────────────────────────────────────────────
public record ScriptError(int LineNumber, string Command, string Message);

public class ScriptEngine
{
    private readonly CommandRegistry _registry;
    private bool _cogoEngineOff = false;

    /// <summary>
    /// Errors reported during the most recent <see cref="ExecuteBatchAsync"/> run.
    /// Reset at the start of each batch execution.
    /// </summary>
    public IReadOnlyList<ScriptError> LastBatchErrors { get; private set; } = [];

    /// <summary>Maximum depth for nested INCLUDE files (prevents infinite recursion).</summary>
    private const int MaxIncludeDepth = 8;

    public ScriptEngine(CommandRegistry registry)
    {
        _registry = registry;
    }

    // ── Single-line execute (interactive Cogo tab) ────────────────────────────
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

    // ── Batch execute (Script tab / Walk) — now with structured error collection ─
    /// <summary>
    /// Executes a multi-line script string.
    /// Collects structured <see cref="ScriptError"/> entries and writes a summary
    /// to the context log at the end of the run.
    /// </summary>
    public async Task ExecuteBatchAsync(string script, ICogoContext context)
    {
        var errors = new List<ScriptError>();
        LastBatchErrors = errors;

        var lines = script.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int lineNumber = 0;

        foreach (var rawLine in lines)
        {
            lineNumber++;
            string line = StripInlineComment(rawLine).TrimStart();

            // Skip blank / comment lines
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("!") || line.StartsWith("//") ||
                line.StartsWith("/") || line.StartsWith(";") ||
                line.StartsWith("--") || line.StartsWith("#")) continue;

            // ── INCLUDE command ─────────────────────────────────────────────
            if (line.StartsWith("INCLUDE", StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteIncludeAsync(line, lineNumber, context, errors, depth: 0);
                continue;
            }

            // Engine-toggle directives
            if (line.Equals("cogo-engine-off", StringComparison.OrdinalIgnoreCase))
            { _cogoEngineOff = true;  context.Log("COGO Engine PAUSED."); continue; }
            if (line.Equals("cogo-engine-on",  StringComparison.OrdinalIgnoreCase))
            { _cogoEngineOff = false; context.Log("COGO Engine RESUMED."); continue; }
            if (line.StartsWith("pipe-engine-", StringComparison.OrdinalIgnoreCase)) continue;

            if (_cogoEngineOff) continue;

            var args = Tokenize(line);
            if (args.Count == 0) continue;

            string commandName = args[0];

            try
            {
                var command = _registry.GetCommand(commandName);
                if (command != null)
                {
                    await command.ExecuteAsync(args.ToArray(), context);
                }
                else
                {
                    // Numeric-first implicit PT / CONT
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
                                continue;
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
                                continue;
                            }
                        }
                    }

                    if (commandName.Length >= 2 && commandName.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                    {
                        var err = new ScriptError(lineNumber, commandName, $"Unknown command: {commandName}");
                        errors.Add(err);
                        context.Log($"[WARN  L{lineNumber:D4}] Unknown command: {commandName}");
                    }
                }
            }
            catch (Exception ex)
            {
                var err = new ScriptError(lineNumber, commandName, ex.Message);
                errors.Add(err);
                context.Log($"[ERROR L{lineNumber:D4}] {commandName} — {ex.Message}");
            }
        }

        // ── Batch summary ───────────────────────────────────────────────────
        WriteBatchSummary(errors, lineNumber, context);
    }

    // ── INCLUDE handler ───────────────────────────────────────────────────────
    private async Task ExecuteIncludeAsync(
        string line, int parentLineNumber,
        ICogoContext context, List<ScriptError> errors, int depth)
    {
        if (depth >= MaxIncludeDepth)
        {
            var depthErr = new ScriptError(parentLineNumber, "INCLUDE", "Maximum INCLUDE nesting depth exceeded.");
            errors.Add(depthErr);
            context.Log($"[ERROR L{parentLineNumber:D4}] INCLUDE — max nesting depth ({MaxIncludeDepth}) exceeded.");
            return;
        }

        // Parse: INCLUDE "path/to/file.cogo"  or  INCLUDE path/to/file.cogo
        string rest = line.Substring("INCLUDE".Length).Trim().Trim('"').Trim('\'');
        if (string.IsNullOrWhiteSpace(rest))
        {
            errors.Add(new ScriptError(parentLineNumber, "INCLUDE", "No file path specified."));
            context.Log($"[ERROR L{parentLineNumber:D4}] INCLUDE — no file path specified.");
            return;
        }

        // Resolve relative to the project directory if available, then CWD
        string resolved = rest;
        if (!Path.IsPathRooted(rest))
        {
            string baseDir = context.ProjectDirectory ?? Directory.GetCurrentDirectory();
            resolved = Path.Combine(baseDir, rest);
        }

        if (!File.Exists(resolved))
        {
            errors.Add(new ScriptError(parentLineNumber, "INCLUDE", $"File not found: {resolved}"));
            context.Log($"[ERROR L{parentLineNumber:D4}] INCLUDE — file not found: {resolved}");
            return;
        }

        context.Log($"[INCLUDE] Loading: {Path.GetFileName(resolved)}");

        string subScript;
        try   { subScript = await File.ReadAllTextAsync(resolved); }
        catch (Exception ex)
        {
            errors.Add(new ScriptError(parentLineNumber, "INCLUDE", ex.Message));
            context.Log($"[ERROR L{parentLineNumber:D4}] INCLUDE — could not read file: {ex.Message}");
            return;
        }

        // Execute the included script lines inline (recursive, depth-guarded)
        var subLines = subScript.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        int subLine = 0;
        foreach (var rawSubLine in subLines)
        {
            subLine++;
            string sl = StripInlineComment(rawSubLine).TrimStart();
            if (string.IsNullOrWhiteSpace(sl)) continue;
            if (sl.StartsWith("!") || sl.StartsWith("//") || sl.StartsWith("/") ||
                sl.StartsWith(";") || sl.StartsWith("--") || sl.StartsWith("#")) continue;

            if (sl.StartsWith("INCLUDE", StringComparison.OrdinalIgnoreCase))
            {
                await ExecuteIncludeAsync(sl, subLine, context, errors, depth + 1);
                continue;
            }

            if (sl.Equals("cogo-engine-off", StringComparison.OrdinalIgnoreCase))
            { _cogoEngineOff = true;  context.Log("COGO Engine PAUSED (from INCLUDE)."); continue; }
            if (sl.Equals("cogo-engine-on",  StringComparison.OrdinalIgnoreCase))
            { _cogoEngineOff = false; context.Log("COGO Engine RESUMED (from INCLUDE)."); continue; }
            if (sl.StartsWith("pipe-engine-", StringComparison.OrdinalIgnoreCase)) continue;
            if (_cogoEngineOff) continue;

            var args = Tokenize(sl);
            if (args.Count == 0) continue;
            string commandName = args[0];

            try
            {
                var command = _registry.GetCommand(commandName);
                if (command != null)
                    await command.ExecuteAsync(args.ToArray(), context);
                else if (commandName.Length >= 2 && commandName.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                {
                    errors.Add(new ScriptError(subLine, commandName, $"Unknown command in included file: {commandName}"));
                    context.Log($"[WARN  INC:{Path.GetFileName(resolved)}:L{subLine}] Unknown: {commandName}");
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ScriptError(subLine, commandName, ex.Message));
                context.Log($"[ERROR INC:{Path.GetFileName(resolved)}:L{subLine}] {commandName} — {ex.Message}");
            }
        }

        context.Log($"[INCLUDE] Done: {Path.GetFileName(resolved)} ({subLine} lines)");
    }

    // ── Batch summary writer ──────────────────────────────────────────────────
    private static void WriteBatchSummary(List<ScriptError> errors, int totalLines, ICogoContext context)
    {
        int errCount  = errors.Count(e => !e.Message.StartsWith("Unknown"));
        int warnCount = errors.Count(e => e.Message.StartsWith("Unknown"));

        if (errors.Count == 0)
        {
            context.Log($"[BATCH] Completed {totalLines} lines — no errors.");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[BATCH] Completed {totalLines} lines — {errCount} error(s), {warnCount} warning(s).");
        sb.AppendLine("[BATCH] Error Summary:");

        foreach (var e in errors.Take(25))   // Cap at 25 lines in log
            sb.AppendLine($"  L{e.LineNumber:D4}  [{e.Command,-10}]  {e.Message}");

        if (errors.Count > 25)
            sb.AppendLine($"  ... and {errors.Count - 25} more. Check ErrorReportWindow for full details.");

        context.Log(sb.ToString());
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
