using System;
using System.Collections.Generic;
using System.Linq;

namespace RCS.Cogo.AI;

public class AiAnalysisResult
{
    public int LineNumber { get; set; }
    public string OriginalLine { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info"; // Error, Warning, Suggestion
    public string Message { get; set; } = string.Empty;
    public string SuggestedCorrection { get; set; } = string.Empty;
}

public class AiAnalyzer
{
    private static readonly HashSet<string> ValidCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CLEAR", "DEL", "NE", "NEZ", "INV", "DIR", "AZAZ", "BDBD", "AZDIST", "BDDIST", 
        "CURVE", "MAPCHK", "PTC", "TRAVERSE", "OFFSET", "INVERSE", "AL",
        "PRUN", "SS-B", "SS-C", "SS-E", "SM-C", "SM-E", "BD", "BEG", "BS", "CONT", "DD",
        "COGO-ENGINE-ON", "COGO-ENGINE-OFF", "PIPE-ENGINE-ON", "PIPE-ENGINE-OFF",
        "END", "F1", "F2", "LNLN", "LOAD", "PNT", "PT", "POINT", "RKRK", "SAVE", "SAVE-HALN", "SAVE-PFL", "STN", "TRAV", "XC", "ZD",
        "AD", "AP", "ARCARC", "DISP", "LIST", "START", "CLOSE", "OC", "FS", "FIG", "A", "B", "L", "C", "D",
        "ALGN", "PROF", "VPI", "HALBL-ON", "HALBL-OFF",
        "RESET", "RESET-ON", "RESET-OFF",
        "UNITS", "ATMOS", "TEMP", "PRESS", "SF", "CR", "ANGLES", "VERT", "HORIZ", "EDM", "PRISM", "COLL",
        "LOG", "ECHO", "ABOUT", "SHOW", "SET", "REPORT", "CLOSURE", "TURN", "PTOFFSET", "OFFSETLINE", "CP", "TRN", "ANG", "DIST", "AZ", "MAPCHECK"
    };

    public List<AiAnalysisResult> AnalyzeScript(string scriptText)
    {
        var results = new List<AiAnalysisResult>();
        if (string.IsNullOrWhiteSpace(scriptText)) return results;

        var lines = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        for (int i = 0; i < lines.Length; i++)
        {
            string rawLine = lines[i];
            string line = rawLine.Trim();
            
            if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("!"))
                continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            string cmd = parts[0].ToUpperInvariant();
            
            // Ignore bare numbers (could be implicit PNT commands in some scripts)
            if (double.TryParse(cmd, out _))
                continue;
            
            // Allow pipe directives dynamically like W-C, E-B, WW-E, ST-C
            bool isPipeDirective = cmd.EndsWith("-B") || cmd.EndsWith("-C") || cmd.EndsWith("-E");

            // 1. Unknown Command Check
            if (!ValidCommands.Contains(cmd) && !isPipeDirective)
            {
                // Attempt to find closest match
                string closest = ValidCommands.OrderBy(c => LevenshteinDistance(cmd, c)).First();
                results.Add(new AiAnalysisResult
                {
                    LineNumber = i + 1,
                    OriginalLine = rawLine,
                    Severity = "Error",
                    Message = $"Unknown command '{cmd}'.",
                    SuggestedCorrection = $"Did you mean '{closest}'?"
                });
                continue;
            }

            // 2. Syntax & Parameter checks
            CheckCommandArguments(cmd, parts, i + 1, rawLine, results);
        }

        if (results.Count == 0)
        {
            results.Add(new AiAnalysisResult
            {
                LineNumber = 0,
                OriginalLine = "Entire Script",
                Severity = "Success",
                Message = "Analysis found zero errors or warnings! Great job.",
                SuggestedCorrection = "Execute script when ready."
            });
        }

        return results;
    }

    private void CheckCommandArguments(string cmd, string[] args, int lineNum, string rawLine, List<AiAnalysisResult> results)
    {
        void AddError(string msg, string corr) => results.Add(new AiAnalysisResult { LineNumber = lineNum, OriginalLine = rawLine, Severity = "Error", Message = msg, SuggestedCorrection = corr });
        void AddWarn(string msg, string corr) => results.Add(new AiAnalysisResult { LineNumber = lineNum, OriginalLine = rawLine, Severity = "Warning", Message = msg, SuggestedCorrection = corr });
        
        bool IsNum(string s) => double.TryParse(s, out _);

        switch (cmd)
        {
            case "NE":
                if (args.Length < 3) AddError("NE requires Northing, Easting.", "NE [id] <northing> <easting> [description]");
                else 
                {
                    int startIdx = IsNum(args[1]) && args.Length >= 3 && IsNum(args[2]) ? 1 : 2;
                    if (startIdx == 2 && args.Length < 4) AddError("NE requires ID, Northing, Easting.", "NE <id> <northing> <easting> [description]");
                    else if (!IsNum(args[startIdx]) || !IsNum(args[startIdx+1])) AddError("Northing and Easting must be numeric.", "Ensure valid numeric coordinates.");
                }
                break;
            case "NEZ":
                if (args.Length < 4) AddError("NEZ requires Northing, Easting, Elevation.", "NEZ [id] <northing> <easting> <elev> [desc]");
                else
                {
                    int startIdx = IsNum(args[1]) && args.Length >= 4 && IsNum(args[2]) && IsNum(args[3]) ? 1 : 2;
                    if (startIdx == 2 && args.Length < 5) AddError("NEZ requires ID, Northing, Easting, Elevation.", "NEZ <id> <northing> <easting> <elev> [desc]");
                    else if (!IsNum(args[startIdx]) || !IsNum(args[startIdx+1]) || !IsNum(args[startIdx+2])) AddError("Northing, Easting, Elevation must be numeric.", "Ensure valid numeric coordinates.");
                }
                break;
            case "INV":
                if (args.Length < 3) AddError("INV requires two Point IDs.", "INV <point1> <point2>");
                break;
            case "AZAZ":
                if (args.Length < 6) AddError("AZAZ requires NewPointId, Pt1, Az1, Pt2, Az2.", "AZAZ <new_id> <pt1> <az1> <pt2> <az2>");
                else if (args.Length >= 6 && (!IsNum(args[3]) || !IsNum(args[5]))) AddWarn("Azimuths should typically be numeric degrees.", "Verify azimuth arguments are numbers.");
                break;
            case "BDBD":
                if (args.Length < 6) AddError("BDBD requires NewPointId, Pt1, Brg1, Pt2, Brg2.", "BDBD <new_id> <pt1> <brg1> <pt2> <brg2>");
                else if (args.Length >= 6 && (!args[3].Contains("-") || !args[5].Contains("-"))) AddWarn("Bearings usually contain '-' like 1-45.0000-2 (NE).", "Use quadrant format X-DD.MMSS-Y");
                break;
            case "MAPCHK":
                if (args.Length < 2) AddError("MAPCHK requires a Point ID or Figure Name.", "MAPCHK <figureName / point1 point2 ...>");
                break;
            case "PRUN":
                if (args.Length < 2) AddError("PRUN requires START or END.", "PRUN START <type> <material> <size> ... OR PRUN END");
                else if (args[1].ToUpper() == "START" && args.Length < 5) AddWarn("PRUN START recommends <type> <material> <size>.", "PRUN START W DI 8");
                break;
            case "DEL":
                if (args.Length < 2) AddError("DEL requires a target (PTS, FIG, RUNS, etc).", "DEL PTS or DEL FIG");
                else if (args[1].ToUpper() != "PTS" && args[1].ToUpper() != "FIG") AddWarn("DEL target is unusual.", "Standard targets are PTS or FIG");
                break;
            case "PT":
            case "PNT":
            case "POINT":
                if (args.Length < 4) AddError($"{cmd} requires ID, Northing, Easting.", $"{cmd} <id> <northing> <easting> [elev] [desc]");
                else
                {
                    if (!IsNum(args[2]) || !IsNum(args[3])) AddError("Northing and Easting must be numeric.", "Ensure valid numeric coordinates.");
                }
                break;
            case "ALGN":
                if (args.Length < 2) AddError("ALGN requires a subcommand (BEG, TANGENT, CURVE, END).", "ALGN BEG <name> <station>");
                break;
            case "PROF":
                if (args.Length < 2) AddError("PROF requires a subcommand (BEG, END).", "PROF BEG <alignmentName> <profileName>");
                break;
            case "VPI":
                if (args.Length < 3) AddError("VPI requires Station and Elevation.", "VPI <station> <elevation> [curveLength]");
                break;
            case "LOG":
                if (args.Length < 2) AddError("LOG requires ON or OFF.", "LOG ON or LOG OFF.");
                else if (args[1].ToUpper() != "ON" && args[1].ToUpper() != "OFF") AddWarn("LOG argument should be ON or OFF.", "LOG ON or LOG OFF");
                break;
            case "SAVE-HALN":
                if (args.Length < 2) AddError("SAVE-HALN requires an Alignment Name.", "SAVE-HALN <name> [description]");
                break;
            case "SAVE-PFL":
                if (args.Length < 2) AddError("SAVE-PFL requires a Profile Name.", "SAVE-PFL <name> [description]");
                break;
        }
    }

    private int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t)) return s.Length;

        int[] v0 = new int[t.Length + 1];
        int[] v1 = new int[t.Length + 1];

        for (int i = 0; i < v0.Length; i++) v0[i] = i;

        for (int i = 0; i < s.Length; i++)
        {
            v1[0] = i + 1;
            for (int j = 0; j < t.Length; j++)
            {
                int cost = (s[i] == t[j]) ? 0 : 1;
                v1[j + 1] = Math.Min(Math.Min(v1[j] + 1, v0[j + 1] + 1), v0[j] + cost);
            }
            for (int j = 0; j < v0.Length; j++) v0[j] = v1[j];
        }
        return v1[t.Length];
    }
}
