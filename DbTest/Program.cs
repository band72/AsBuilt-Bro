using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App;
using RCS.Cogo.App.State;
using RCS.Cogo.App.Scripting;
using RCS.Piping.Core.Scripting;

// ============================================================
//  RCS COGO Script Automated Test Runner
//  Tests all SampleScripts against the live COGO + Pipe engines
// ============================================================

Console.OutputEncoding = System.Text.Encoding.UTF8;

var sampleDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SampleScripts");
sampleDir = Path.GetFullPath(sampleDir);

if (!Directory.Exists(sampleDir))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERROR: SampleScripts folder not found at: {sampleDir}");
    return;
}

var scripts = Directory.GetFiles(sampleDir, "*.txt")
    .Where(f => Path.GetFileName(f).StartsWith("0") || Path.GetFileName(f).StartsWith("1"))
    .OrderBy(f => f)
    .ToList();

// Also add Survey tutorial scripts
var surveyDir = Path.Combine(sampleDir, "Survey");
if (Directory.Exists(surveyDir))
{
    scripts.AddRange(Directory.GetFiles(surveyDir, "T*.txt").OrderBy(f => f));
}

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("==========================================================");
Console.WriteLine("  RCS COGO Enterprise — Automated Script Test Runner");
Console.WriteLine($"  Found {scripts.Count} scripts in: {sampleDir}");
Console.WriteLine("==========================================================");
Console.ResetColor();

int totalErrors = 0;
int totalWarnings = 0;
int passCount = 0;
int failCount = 0;

var registry = AppInitializer.InitializeRegistry();
var compiler = new PipeScriptCompiler();

foreach (var scriptPath in scripts)
{
    var scriptName = Path.GetFileName(scriptPath);
    var scriptText = File.ReadAllText(scriptPath);

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine($"\n--- Testing: {scriptName} ---");
    Console.ResetColor();

    // ---- COGO Engine pass ----
    var logs = new List<string>();
    var ctx = new CogoContext(msg => logs.Add(msg));
    var engine = new ScriptEngine(registry);

    int cogoErrors = 0;
    var lines = scriptText.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        var line = lines[i].Trim().TrimEnd('\r');
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
        try
        {
            await engine.ExecuteAsync(line, ctx);
        }
        catch (Exception ex)
        {
            logs.Add($"[EXCEPTION Line {i + 1}] {ex.Message}");
        }
    }

    // Count COGO errors
    foreach (var log in logs)
    {
        if (log.StartsWith("Error:", StringComparison.OrdinalIgnoreCase) ||
            log.Contains("[EXCEPTION"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [COGO ERROR] {log}");
            Console.ResetColor();
            cogoErrors++;
            totalErrors++;
        }
    }

    int cogoPoints = ctx.GetAllPoints().Count();
    int cogoFigures = ctx.GetAllFigures().Count();
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine($"  COGO: {cogoPoints} points, {cogoFigures} figures, {cogoErrors} errors");
    Console.ResetColor();

    // ---- Pipe Engine pass ----
    // Build valid material/code sets (empty = skip validation, accept all)
    var validMaterials = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "PVC","DIP","PE","ALUM","HDPE","CMP","RCP","ABS","CI","GI","SS","CPVC","CONC","CLAY","STEEL",
        "NONE","UNKNOWN"
    };
    var validCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "W","WW","S","R","G","E","EL","ST","CH","D","SS","EP","SP",
        "WAT","WM","WV","WF","HYD","WMET","WBFP","WBO","WAR","SEW","WWM","WWV","WWF",
        "STORM","DRAIN","STV","STF","STM","CBI","DI","HW",
        "ELEC","EV","EF","EM","EMET","WPP","EPOLE","EBOX","POLE","BOX",
        "GAS","GV","GF","GM","GMET","REC","RV","RF","RM","CHIL","CHV","CHF","CHM",
        "CORNER","BLDG"
    };

    var pipeResult = await Task.Run(() =>
        compiler.Compile(scriptText, id => ctx.GetPoint(id), validMaterials, validCodes));

    int pipeErrors   = pipeResult.Diagnostics.Count(d => d.Severity == "ERROR");
    int pipeWarnings = pipeResult.Diagnostics.Count(d => d.Severity == "WARN");
    int pipeRuns     = pipeResult.Runs.Count;
    int structs      = pipeResult.Structures.Count;

    foreach (var diag in pipeResult.Diagnostics.Where(d => d.Severity == "ERROR"))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  [PIPE  ERROR  L{diag.LineNumber:000}] {diag.Message}");
        Console.ResetColor();
        totalErrors++;
    }
    foreach (var diag in pipeResult.Diagnostics.Where(d => d.Severity == "WARN"))
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  [PIPE  WARN   L{diag.LineNumber:000}] {diag.Message}");
        Console.ResetColor();
        totalWarnings++;
    }

    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine($"  PIPE: {pipeRuns} runs, {structs} structures, {pipeErrors} errors, {pipeWarnings} warnings");
    Console.ResetColor();

    bool scriptPassed = (cogoErrors == 0 && pipeErrors == 0);
    if (scriptPassed)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ PASS");
        passCount++;
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ FAIL  ({cogoErrors} COGO errors + {pipeErrors} pipe errors)");
        failCount++;
    }
    Console.ResetColor();
}

// Summary
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("\n==========================================================");
Console.WriteLine($"  RESULTS:  {passCount} PASSED  |  {failCount} FAILED");
Console.WriteLine($"  Total Errors: {totalErrors}   Total Warnings: {totalWarnings}");
Console.WriteLine("==========================================================");
Console.ResetColor();

if (totalErrors > 0)
    Environment.Exit(1); // Non-zero exit for CI detection
