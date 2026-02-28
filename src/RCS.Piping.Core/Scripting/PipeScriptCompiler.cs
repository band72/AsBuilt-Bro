using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RCS.Piping.Core.Models;

namespace RCS.Piping.Core.Scripting;

/// <summary>
/// Strict Civil-style script compiler for pipe connectivity.
/// </summary>
public sealed class PipeScriptCompiler
{
    private static readonly HashSet<string> KnownDirectives = new(StringComparer.OrdinalIgnoreCase)
    {
        "B","C","E","CLS",
        "BC","EC","OC","CIR",
        "H","V","SO",
        "RPN","CPN","RECT","RT","X"
    };

    public ScriptCompileResult Compile(
        string script,
        Func<string, RCS.Cogo.Core.Primitives.Point3D?> getPoint,
        HashSet<string> validMaterials,
        HashSet<string> validCodes)
    {
        var result = new ScriptCompileResult();

        if (string.IsNullOrWhiteSpace(script))
        {
            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = 1, Severity = "INFO", Message = "Script is empty." });
            return result;
        }

        var lines = script.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        // Current PRUN context (if any)
        PrunContext? prun = null;
        bool pipeEngineOff = false;
        var localPoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNo = i + 1;
            var raw = lines[i];

            // Strip comments (allow ; or //)
            var line = StripComments(raw).Trim();
            if (line.Length == 0)
                continue;

            try
            {
                var tokens = Tokenize(line);
                if (tokens.Count == 0)
                    continue;

                string cmd = tokens[0].ToUpperInvariant();

                // Engine Toggles
                if (cmd == "PIPE-ENGINE-OFF")
                {
                    pipeEngineOff = true;
                    result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = "Pipe Engine Scripting PAUSED." });
                    continue;
                }
                if (cmd == "PIPE-ENGINE-ON")
                {
                    pipeEngineOff = false;
                    result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = "Pipe Engine Scripting RESUMED." });
                    continue;
                }
                
                // Allow unified scripts without throwing errors in the Pipe Engine for COGO toggles
                if (cmd == "COGO-ENGINE-OFF" || cmd == "COGO-ENGINE-ON" || 
                    cmd == "RESET" || cmd == "RESET-OFF" || cmd == "RESET-ON" || cmd == "CLEAR" || cmd == "ECHO" || cmd == "LOG" || cmd == "LIST" || cmd == "REPORT" || cmd == "ABOUT" || cmd == "SET" || cmd == "SHOW" || cmd == "UNITS" || cmd == "ANGLES")
                    continue;

                // Track local COGO point creations so unified scripts don't fail cross-validation
                if ((cmd == "NE" || cmd == "NEZ" || cmd == "PNT" || cmd == "PT") && tokens.Count > 1)
                {
                    localPoints.Add(tokens[1]);
                    // Let it fall through, but if pipe engine isn't off, we don't want it to error as "Unknown Command" if it's a structural point.
                    if (!pipeEngineOff) continue; 
                }

                if (pipeEngineOff)
                    continue;

                // 1. PRUN commands
                if (cmd == "PRUN")
                {
                    HandlePrun(tokens, lineNo, ref prun, result, validMaterials, validCodes);
                    continue;
                }

                // 2. Numeric Pipe Segment (only valid inside PRUN)
                if (double.TryParse(tokens[0], out _))
                {
                    if (prun != null)
                    {
                        bool PointExistsWrapper(string id) => localPoints.Contains(id) || getPoint(id) != null;
                        HandlePrunSegment(tokens, lineNo, prun, PointExistsWrapper, result, getPoint);
                        continue;
                    }
                    else
                    {
                        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Unexpected numeric token '{tokens[0]}' outside PRUN context." });
                        continue;
                    }
                }

                // 3. Special: SS-C (Store Structure)
                // Usage: SS-C <PointID> <Code>
                if (tokens[0].Equals("SS-C", StringComparison.OrdinalIgnoreCase))
                {
                    if (tokens.Count < 3)
                    {
                        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = "SS-C requires PointID and Code." });
                        continue;
                    }
                    var ptId = tokens[1];
                    var code = tokens[2];
                    
                    bool PointExistsWrapper(string id) => localPoints.Contains(id) || getPoint(id) != null;

                    if (!PointExistsWrapper(ptId))
                    {
                        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Point {ptId} not found for SS-C." });
                        continue;
                    }

                    if (validCodes.Count > 0 && !validCodes.Contains(code))
                    {
                        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = $"Structure Code '{code}' is not recognized in the Master Database." });
                    }

                    AddStructure(ptId, code, result);
                    result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = $"Added Structure {code} at {ptId}." });
                    continue;
                }

                // 4. Linework FEATURE-DIRECTIVE (e.g. EP-B, EP-C)
                if (TryParseFeatureDirective(tokens[0], out var feature, out var directive, out var fdError))
                {
                    if (!KnownDirectives.Contains(directive))
                    {
                        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Unknown directive '{directive}' in '{tokens[0]}'." });
                        continue;
                    }

                    if (prun == null)
                    {
                        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = $"Linework '{tokens[0]}' ignored outside PRUN context (no active Utility)." });
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(prun.Feature) && !feature.Equals(prun.Feature, StringComparison.OrdinalIgnoreCase) && !feature.Equals(prun.UtilityType, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Feature '{feature}' mismatch. Expected '{prun.Feature}-...' or '{prun.UtilityType}-...'." });
                        continue;
                    }

                    // Wrap pointExists to include localPoints
                    bool PointExistsWrapper(string id) => localPoints.Contains(id) || getPoint(id) != null;

                    HandleLinework(feature, directive, tokens, lineNo, prun, PointExistsWrapper, result, getPoint);
                    continue;
                }

                if (!string.IsNullOrEmpty(fdError))
                {
                     result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = fdError });
                     continue;
                }

                // 5. Unknown
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Unknown command '{tokens[0]}'." });
            }
            catch (Exception ex)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Internal error: {ex.Message}" });
            }
        }

        // If PRUN left open, warn.
        if (prun != null)
        {
            result.Diagnostics.Add(new ScriptDiagnostic
            {
                LineNumber = lines.Length,
                Severity = "WARN",
                Message = "PRUN context was not closed with 'PRUN END'."
            });
        }

        return result;
    }

    private static void HandlePrun(List<string> tokens, int lineNo, ref PrunContext? prun, ScriptCompileResult result, HashSet<string> validMaterials, HashSet<string> validCodes)
    {
        if (tokens.Count < 2)
        {
            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = "PRUN missing verb. Use: PRUN START ... or PRUN END" });
            return;
        }

        var verb = tokens[1];
        if (verb.Equals("START", StringComparison.OrdinalIgnoreCase))
        {
            if (prun != null)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = "PRUN START encountered while another PRUN is active. Close it with PRUN END." });
                return;
            }

            // PRUN START <UTIL> DIAM <d> MAT <m> [FIG <FEATURE>]
            if (tokens.Count < 6)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = "PRUN START syntax: PRUN START <UTIL> DIAM <diam> MAT <material> [FIG <FEATURE>]" });
                return;
            }

            var util = tokens[2].Trim().ToUpperInvariant(); // Use string for UtilityType
            
            // Find DIAM and MAT
            double? diam = null;
            string? mat = null;
            string? fig = null;

            for (int i = 3; i < tokens.Count; i++)
            {
                if (tokens[i].Equals("DIAM", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Count)
                {
                    if (double.TryParse(tokens[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                        diam = d;
                    else
                        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Invalid DIAM value '{tokens[i + 1]}'" });
                    i++;
                    continue;
                }

                if (tokens[i].Equals("MAT", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Count)
                {
                    mat = tokens[i + 1];
                    i++;
                    continue;
                }

                if (tokens[i].Equals("FIG", StringComparison.OrdinalIgnoreCase) && i + 1 < tokens.Count)
                {
                    fig = tokens[i + 1];
                    i++;
                    continue;
                }
            }

            if (diam == null)
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = "PRUN START missing DIAM; runs will be created without diameter." });
            if (string.IsNullOrWhiteSpace(mat))
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = "PRUN START missing MAT; runs will be created without material." });

            prun = new PrunContext
            {
                UtilityType = util,
                Diameter = diam,
                Material = mat,
                Feature = fig,
            };

            if (!string.IsNullOrWhiteSpace(mat) && validMaterials.Count > 0 && !validMaterials.Contains(mat))
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Material '{mat}' is not recognized in the Master Database catalog!" });
                prun = null; // Abort PRUN block creation to simulate strict fail
                return;
            }

            if (!string.IsNullOrWhiteSpace(util) && validCodes.Count > 0 && !validCodes.Contains(util))
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = $"Utility type '{util}' may not be recognized strictly in validation lists." });
            }

            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = $"PRUN started: {util} DIAM={diam?.ToString(CultureInfo.InvariantCulture) ?? ""} MAT={mat ?? ""} FIG={fig ?? ""}" });
            return;
        }

        if (verb.Equals("END", StringComparison.OrdinalIgnoreCase))
        {
            if (prun == null)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = "PRUN END encountered but no PRUN is active." });
                return;
            }

            // Flush any open figure logic if needed
            prun = null;
            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = "PRUN ended." });
            return;
        }

        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Unknown PRUN verb '{verb}'. Use START or END." });
    }

    private static void HandleLinework(
        string feature,
        string directive,
        List<string> tokens,
        int lineNo,
        PrunContext prun,
        Func<string, bool> pointExists,
        ScriptCompileResult result,
        Func<string, RCS.Cogo.Core.Primitives.Point3D?> getPoint)
    {
        if (directive.Equals("B", StringComparison.OrdinalIgnoreCase))
        {
            if (tokens.Count < 2)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"{feature}-B requires a point number/ID." });
                return;
            }

            var ptId = tokens[1];
            if (!pointExists(ptId))
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Point {ptId} not found for {feature}-B." });
                return;
            }

            prun.Vertices.Clear();
            prun.Vertices.Add(ptId);
            prun.StartVertex = ptId;
            prun.IsFigureActive = true;
            
            // Add structure at start
            AddStructure(ptId, prun.UtilityType, result);
            
            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = $"{feature} begin at point {ptId}." });
            return;
        }

        if (directive.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            if (!prun.IsFigureActive)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"{feature}-C used before {feature}-B." });
                return;
            }

            if (tokens.Count < 2)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"{feature}-C requires a point number/ID." });
                return;
            }

            var ptId = tokens[1];
            if (!pointExists(ptId))
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Point {ptId} not found for {feature}-C." });
                return;
            }

            var prev = prun.Vertices.Last();
            
            // Check if there's an explicit structure type mapped like 'SS-C 1 Manhole'
            string explicitType = prun.UtilityType;
            if (tokens.Count > 2 && !double.TryParse(tokens[2], out _))
            {
                explicitType = tokens[2];
            }

            if (prev == ptId)
            {
                // Just an explicit structural update on the same node
                AddStructure(ptId, explicitType, result);
                return;
            }

            prun.Vertices.Add(ptId);
            
            // Generate PipeRun
            var run = BuildRun(prun, prev, ptId);
            result.Runs.Add(run);
            
            // Add/Update structure at node
            AddStructure(ptId, explicitType, result);
            
            return;
        }

        if (directive.Equals("CLS", StringComparison.OrdinalIgnoreCase))
        {
            if (!prun.IsFigureActive)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"{feature}-CLS used before {feature}-B." });
                return;
            }

            if (prun.Vertices.Count < 2 || prun.StartVertex == null)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = $"{feature}-CLS ignored (not enough vertices)." });
                return;
            }

            var last = prun.Vertices.Last();
            var first = prun.StartVertex; // Since string? is nullable
            if (first != null && last != first)
            {
                result.Runs.Add(BuildRun(prun, last, first));
                // Usually structures exist for first/last already.
            }
                
            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = $"{feature} closed." });
            return;
        }

        if (directive.Equals("E", StringComparison.OrdinalIgnoreCase))
        {
            if (!prun.IsFigureActive)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = $"{feature}-E encountered but figure was not active." });
                return;
            }

            prun.IsFigureActive = false;
            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = $"{feature} end." });
            return;
        }


        // Everything else not implemented
        result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = $"Directive {feature}-{directive} recognized but not implemented." });
    }

    private static void AddStructure(string ptId, string type, ScriptCompileResult result)
    {
        var existing = result.Structures.FirstOrDefault(s => s.PointId == ptId);
        if (existing == null)
        {
            result.Structures.Add(new PipeStructure 
            { 
               PointId = ptId,
               Type = type 
            });
        }
        else
        {
            // If the script gives us a specific type (e.g. Manhole), overwrite the generic utility prefix
            if (!string.IsNullOrWhiteSpace(type) && type.Length > 2)
            {
                existing.Type = type;
            }
        }
    }

    private static PipeRun BuildRun(PrunContext prun, string fromPt, string toPt)
    {
        var diam = prun.Diameter ?? 0.0;
        
        // Construct PipeRun
        return new PipeRun
        {
            Type = prun.UtilityType,
            FromPointId = fromPt,
            ToPointId = toPt,
            Diameter = diam,
            Material = prun.Material ?? string.Empty,
            PartKey = $"{prun.UtilityType}|PIPE|{diam}|{prun.Material ?? ""}" // Matches logic somewhat
        };
    }

        private static string StripComments(string line)
        {
            var s = line;
            var semi = s.IndexOf(';');
            if (semi >= 0) s = s[..semi];
            var dbl = s.IndexOf("//", StringComparison.Ordinal);
            if (dbl >= 0) s = s[..dbl];
            if (s.TrimStart().StartsWith("!")) return string.Empty;
            if (s.TrimStart().StartsWith("/")) return string.Empty;
            return s;
        }

        private static void HandlePrunSegment(List<string> tokens, int lineNo, PrunContext prun, Func<string, bool> pointExists, ScriptCompileResult result, Func<string, RCS.Cogo.Core.Primitives.Point3D?> getPoint)
        {
            if (tokens.Count < 2)
            {
                result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = "Pipe segment requires at least From and To points." });
                return;
            }

            string from = tokens[0];
            string to = tokens[1];

            if (!pointExists(from)) result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Point {from} not found." });
            if (!pointExists(to)) result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "ERROR", Message = $"Point {to} not found." });

            double? invStart = null;
            double? invEnd = null;

            if (tokens.Count > 2 && double.TryParse(tokens[2], out var s)) invStart = s;
            if (tokens.Count > 3 && double.TryParse(tokens[3], out var e)) invEnd = e;
            
            // **SLOPE CHECKING LOGIC**
            if (invStart.HasValue && invEnd.HasValue)
            {
                var p1 = getPoint?.Invoke(from);
                var p2 = getPoint?.Invoke(to);
                if (p1 != null && p2 != null)
                {
                    double dx = p2.Easting - p1.Easting;
                    double dy = p2.Northing - p1.Northing;
                    double length = Math.Sqrt(dx * dx + dy * dy);

                    if (length > 0)
                    {
                        double drop = invStart.Value - invEnd.Value;
                        double slopePercent = (drop / length) * 100.0;

                        if ((prun.UtilityType == "WW" || prun.UtilityType == "ST" || prun.UtilityType == "D") && slopePercent < 0.40)
                        {
                            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "WARN", Message = $"Slope warning: Gravity Pipe {from}-{to} is {slopePercent:F2}%. Standard minimum allowable slope is 0.40%." });
                        }
                    }
                }
            }

            var run = new PipeRun
            {
                Type = prun.UtilityType, // W, WW, R
                FromPointId = from,
                ToPointId = to,
                Diameter = prun.Diameter ?? 0,
                Material = prun.Material ?? "",
                InvertStart = invStart ?? 0,
                InvertEnd = invEnd ?? 0,
                PartKey = $"Pipe-{prun.UtilityType}-{prun.Diameter}" // Temporary PartKey
            };
            result.Runs.Add(run);
            result.Diagnostics.Add(new ScriptDiagnostic { LineNumber = lineNo, Severity = "INFO", Message = $"Added Pipe {from}-{to}." });
        }

    private static List<string> Tokenize(string line)
    {
        return line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static bool TryParseFeatureDirective(string token, out string feature, out string directive, out string? error)
    {
        feature = string.Empty;
        directive = string.Empty;
        error = null;

        var idx = token.IndexOf('-');
        if (idx < 0) return false;

        // Strict "no extra spaces": token must be exactly FEATURE-DIRECTIVE
        if (token.Contains(" -") || token.Contains("- "))
        {
            error = $"Invalid token '{token}'. Use strict FEATURE-DIRECTIVE with no spaces, e.g. SS-B.";
            return false;
        }

        if (idx == 0 || idx == token.Length - 1)
        {
            error = $"Invalid token '{token}'. Use FEATURE-DIRECTIVE (e.g. SS-B).";
            return false;
        }

        feature = token.Substring(0, idx);
        directive = token.Substring(idx + 1);

        // Validate basic characters
        if (feature.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_')))
        {
            error = $"Invalid feature '{feature}' in token '{token}'.";
            return false;
        }
        if (directive.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_')))
        {
            error = $"Invalid directive '{directive}' in token '{token}'.";
            return false;
        }

        return true;
    }

    private sealed class PrunContext
    {
        public string UtilityType { get; init; } = "Gen";
        public double? Diameter { get; init; }
        public string? Material { get; init; }
        public string? Feature { get; init; }

        public bool IsFigureActive { get; set; }
        public string? StartVertex { get; set; }
        public List<string> Vertices { get; } = new();
    }
}
