using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State;
using RCS.Cogo.Core.Primitives;
using RCS.Cogo.Wpf.Commands;
using RCS.Cogo.App.Models;
using RCS.Cogo.App.Persistence;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using RCS.Geo.Core;
using RCS.Geo.ProjNet;
using RCS.Geo.Abstractions;
using GeoWpf = RCS.Geo.Wpf.ViewModels;

namespace RCS.Cogo.Wpf.ViewModels;

public partial class ShellViewModel
{
    private void AnalyzePipingScript()
    {
        var analyzer = new RCS.Cogo.AI.AiAnalyzer();
        var results = analyzer.AnalyzeScript(PipingScriptText);
        
        var aiWindow = new RCS.Cogo.Wpf.Views.AiAnalysisWindow(results);
        aiWindow.Owner = App.Current.MainWindow;
        aiWindow.ShowDialog();
    }

    private void OpenAiChat()
    {
        var aiWindow = new RCS.Cogo.Wpf.Views.AiScriptChatWindow(PipingScriptText);
        // Item 5: when the chat window extracts a COGO script from a plat image,
        // push it directly into the Script Editor (BatchScriptContent).
        aiWindow.OnScriptExtracted = (extractedScript) =>
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                BatchScriptContent = extractedScript;
                _context.Log("[AUDIT] Plat COGO script loaded into Script Editor from AI Chat image attachment.");
            });
        };
        aiWindow.Owner = App.Current.MainWindow;
        aiWindow.ShowDialog();
    }

    private void ImportPipingScript()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                PipingScriptText = System.IO.File.ReadAllText(dialog.FileName);
                CommandLog.Add($"Loaded piping script: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error reading file: {ex.Message}");
            }
        }
    }

    private void ExportPipingScript()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = "PipingScript.txt"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                System.IO.File.WriteAllText(dialog.FileName, PipingScriptText);
                CommandLog.Add($"Piping script exported to: {dialog.FileName}");
            }
            catch (Exception)
            {
            }
        }
    }

    public System.Windows.Input.ICommand ExportBomCommand { get; }
    public System.Windows.Input.ICommand ExportEpanetCommand { get; }
    public System.Windows.Input.ICommand ExportScheduleCommand { get; }

    private void ExportBom()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "Project_BOM.csv"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                using var writer = new System.IO.StreamWriter(dialog.FileName);
                writer.WriteLine("Type,Discipline,Part/Material,Quantity,Length/Count");

                // Calculate Pipes
                var pipeGroups = PipeRuns.GroupBy(r => new { r.Type, r.Material, r.Diameter });
                foreach(var pg in pipeGroups)
                {
                    double totalLen = 0;
                    foreach(var run in pg)
                    {
                        var p1 = _context.GetPoint(run.FromPointId);
                        var p2 = _context.GetPoint(run.ToPointId);
                        if (p1 != null && p2 != null)
                        {
                            double dx = p2.Easting - p1.Easting;
                            double dy = p2.Northing - p1.Northing;
                            totalLen += Math.Sqrt(dx * dx + dy * dy);
                        }
                    }
                    writer.WriteLine($"Pipe,{pg.Key.Type},{pg.Key.Material} Dia:{pg.Key.Diameter},LF,{totalLen:F2}");
                }

                // Calculate Structures
                var structGroups = Structures.GroupBy(s => new { s.Type });
                foreach(var sg in structGroups)
                {
                    writer.WriteLine($"Structure,Mixed,{sg.Key.Type},EA,{sg.Count()}");
                }

                CommandLog.Add($"BOM exported to: {dialog.FileName}");
                _context.Log($"[AUDIT] BOM Exported: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting BOM: {ex.Message}");
            }
        }
    }

    private void ExportEpanet()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "EPANET INP File (*.inp)|*.inp|All Files (*.*)|*.*",
            DefaultExt = ".inp",
            FileName = "PipingNetwork.inp"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new System.IO.StreamWriter(dialog.FileName);
                writer.WriteLine("[TITLE]");
                writer.WriteLine("Generated EPANET Network");
                writer.WriteLine();

                writer.WriteLine("[JUNCTIONS]");
                writer.WriteLine(";ID              Elev        Demand      Pattern         ");
                foreach (var s in Structures)
                {
                    var pt = _context.GetPoint(s.PointId);
                    double elev = pt?.Elevation ?? 0.0;
                    writer.WriteLine($" Node_{s.PointId,-8} {elev,-10:F2} 0           ");
                }
                writer.WriteLine();

                writer.WriteLine("[PIPES]");
                writer.WriteLine(";ID              Node1           Node2           Length      Diameter    Roughness   MinorLoss   Status");
                int pipeId = 1;
                foreach (var r in PipeRuns)
                {
                    var p1 = _context.GetPoint(r.FromPointId);
                    var p2 = _context.GetPoint(r.ToPointId);
                    double len = 100.0; // Default if missing
                    if (p1 != null && p2 != null)
                    {
                        len = Math.Sqrt(Math.Pow(p2.Easting - p1.Easting, 2) + Math.Pow(p2.Northing - p1.Northing, 2));
                    }
                    if (len < 0.1) len = 0.1;

                    writer.WriteLine($" Pipe_{pipeId,-8} Node_{r.FromPointId,-8} Node_{r.ToPointId,-8} {len,-10:F2} {r.Diameter,-10:F2} 150         0           Open");
                    pipeId++;
                }
                writer.WriteLine();

                writer.WriteLine("[COORDINATES]");
                writer.WriteLine(";Node            X-Coord         Y-Coord");
                foreach (var s in Structures)
                {
                    var pt = _context.GetPoint(s.PointId);
                    if (pt != null)
                    {
                        writer.WriteLine($" Node_{s.PointId,-8} {pt.Easting,-15:F2} {pt.Northing,-15:F2}");
                    }
                }
                writer.WriteLine();
                
                writer.WriteLine("[END]");

                CommandLog.Add($"EPANET Network exported to: {dialog.FileName}");
                _context.Log($"[AUDIT] EPANET INP Exported: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting EPANET: {ex.Message}");
            }
        }
    }

    private void ExportSchedule()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            DefaultExt = ".csv",
            FileName = "Appurtenance_Schedule.csv"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var writer = new System.IO.StreamWriter(dialog.FileName);
                writer.WriteLine("NodeID,StructureType,Northing,Easting,Elevation,Notes");

                foreach (var s in Structures)
                {
                    var pt = _context.GetPoint(s.PointId);
                    double n = pt?.Northing ?? 0.0;
                    double e = pt?.Easting ?? 0.0;
                    double z = pt?.Elevation ?? 0.0;
                    
                    writer.WriteLine($"{s.PointId},{s.Type},{n:F2},{e:F2},{z:F2},");
                }

                CommandLog.Add($"Schedule exported to: {dialog.FileName}");
                _context.Log($"[AUDIT] Schedule Exported: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting Schedule: {ex.Message}");
            }
        }
    }

    private async void ProcessPipingScript()
    {
        if (string.IsNullOrWhiteSpace(PipingScriptText)) { CommandLog.Add("Script is empty."); return; }

        IsRunningScript = true;
        try
        {
            await Task.Delay(1500); // UI delay to enforce progress bar visibility
            _context.Log("--- Processing Unified Cogo Context ---");
            var lines = PipingScriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            bool cogoEngineOn = true;
            int counter = 0;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/"))
                    continue;

                string cmdLower = trimmed.ToLowerInvariant();
                
                // Engine Toggles for Pre-processor
                if (cmdLower == "cogo-engine-off")
                {
                    cogoEngineOn = false;
                    _context.Log("Cogo Engine Scripting PAUSED.");
                    continue;
                }
                if (cmdLower == "cogo-engine-on")
                {
                    cogoEngineOn = true;
                    _context.Log("Cogo Engine Scripting RESUMED.");
                    continue;
                }
                if (cmdLower == "pipe-engine-off" || cmdLower == "pipe-engine-on")
                {
                    // We ignore pipe commands in the Cogo context preprocessing
                    continue;
                }

                // Filter out clear Pipe Engine commands to prevent Cogo Engine from logging 'Unknown command'
                var parts = cmdLower.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                bool isPipeCommand = false;
                if (parts.Length > 0)
                {
                    string p0 = parts[0];
                    if (p0 == "prun" || p0.StartsWith("ss-") || p0.EndsWith("-b") || p0.EndsWith("-c") || p0.EndsWith("-e"))
                    {
                        isPipeCommand = true;
                    }
                }

                if (cogoEngineOn && !isPipeCommand)
                {
                    await _engine.ExecuteAsync(trimmed, _context);
                }
                
                counter++;
                if (counter % 50 == 0) await Task.Delay(1); // Keep UI responsive for thousands of lines
            }

            _context.Log("--- Compiling Piping Script ---");
            
            var compiler = new RCS.Piping.Core.Scripting.PipeScriptCompiler();
            
            var validMaterials = new HashSet<string>(MasterCatalog.Select(m => m.Material), StringComparer.OrdinalIgnoreCase);
            
            // Allow generic materials for non-pipe utilities (like power/electric runs)
            validMaterials.Add("NONE");
            validMaterials.Add("UNKNOWN");

            var validCodes = new HashSet<string>(CogoCodes.Select(c => c.LocalCode), StringComparer.OrdinalIgnoreCase);
            
            foreach (var mat in MasterCatalog)
            {
                if (!string.IsNullOrWhiteSpace(mat.FeatureType)) validCodes.Add(mat.FeatureType);
            }

            // Include valid Utility Disciplines explicitly mapped from PRUN defaults
            validCodes.Add("W");
            validCodes.Add("WW");
            validCodes.Add("S");
            validCodes.Add("R");
            validCodes.Add("G");
            validCodes.Add("E");
            validCodes.Add("EL");
            validCodes.Add("ST");
            validCodes.Add("CH");
            validCodes.Add("D");
            
            // Map common structural aliases
            validCodes.Add("POLE");
            validCodes.Add("BOX");
            validCodes.Add("EPOLE");
            validCodes.Add("EBOX");
            validCodes.Add("VAULT");
            validCodes.Add("METER");
            validCodes.Add("MANHOLE");
            validCodes.Add("VALVE");
            validCodes.Add("HYDRANT");

            var scriptTextCapture = PipingScriptText; // Ensure no cross-thread property access
            var result = await Task.Run(() => compiler.Compile(scriptTextCapture, (id) => _context.GetPoint(id), validMaterials, validCodes));

            // Clear existing states so double-execution actually replaces the networks instead of incrementally stacking items
            _pipeNetwork.Clear();
            PipeRuns.Clear();
        Structures.Clear();

        // Process Diagnostics
        foreach (var diag in result.Diagnostics)
        {
            _context.Log(diag.ToString());
        }

        if (result.Diagnostics.Any(d => d.Severity == "ERROR"))
        {
            _context.Log("Compilation failed with errors. No changes made.");
            return;
        }

        // Apply Results
        int runsAdded = 0;
        int structsAdded = 0;

        foreach (var run in result.Runs)
        {
            _pipeNetwork.AddRun(run);
            PipeRuns.Add(run);
            runsAdded++;
        }

        foreach (var str in result.Structures)
        {
            // Only add if not already in UI collection (check by PointId)
            if (!Structures.Any(s => s.PointId == str.PointId))
            {
                _pipeNetwork.AddStructure(str);
                Structures.Add(str);
                structsAdded++;
            }
        }
        
        _context.Log($"Compilation Success: Added {runsAdded} runs and {structsAdded} structures.");
        _context.Log("--- Parsing Complete ---");
        
        RefreshData();
        // Auto-Sync disabled per user request. Use manual sync button.
        // _ = SyncToAssetsAsync(result);
        }
        finally
        {
            IsRunningScript = false;
        }
    }

    public System.Windows.Input.ICommand SaveHorizontalAlignmentCommand { get; }
    public System.Windows.Input.ICommand SaveVerticalAlignmentCommand { get; }
    public System.Windows.Input.ICommand DeleteHorizontalAlignmentCommand { get; }
    public System.Windows.Input.ICommand DeleteVerticalAlignmentCommand { get; }
    public System.Windows.Input.ICommand SyncToAssetsCommand { get; }

    private void SyncAssets()
    {
        // Re-use existing logic, but from current state
        // We need to re-compile or just iterate existing in-memory structures?
        // Existing logic takes ScriptCompileResult. We can probably just iterate PipeRuns and Structures in memory.
        
        // Let's create a dummy result from current state
        var result = new RCS.Piping.Core.Scripting.ScriptCompileResult();
        result.Runs.AddRange(PipeRuns);
        result.Structures.AddRange(Structures);
        
        _ = SyncToAssetsAsync(result);
    }

    private bool HasScriptKey(RCS.Data.Entities.InstalledAsset asset, string key) 
    {
        return asset.SourceSheetRowIndex?.Contains($"[ScriptID:{key}]") == true;
    }

    private string AddScriptKey(string? notes, string key)
    {
        string tag = $"[ScriptID:{key}]";
        if (notes?.Contains(tag) == true) return notes;
        return (notes + " " + tag).Trim();
    }

    private async Task SyncToAssetsAsync(RCS.Piping.Core.Scripting.ScriptCompileResult result)
    {
      try
      {
        _context.SyncPointsAction?.Invoke(); // Make sure new script points get recorded
        _context.Log("--- Syncing to Installed Assets ---");
        int count = 0;
        int updated = 0;

        foreach (var run in result.Runs)
        {
            var pStart = _context.GetPoint(run.FromPointId);
            var pEnd = _context.GetPoint(run.ToPointId);
            double n1 = pStart?.Northing ?? 0;
            double e1 = pStart?.Easting ?? 0;
            double n2 = pEnd?.Northing ?? 0;
            double e2 = pEnd?.Easting ?? 0;

            string type = (run.Type ?? "").ToUpper();
            string key = $"Run-{run.Id}"; 
            
            // Checks
            RCS.Data.Entities.InstalledAsset? existing = null;
            RCS.Data.Entities.InstalledAsset? assetToSave = null;

            if (type == "W") 
            {
                 var specific = InstalledAssets.WaterPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.WaterPipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material;
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                 
                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "WW")
            {
                 var specific = InstalledAssets.WWGravityPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.WWGravityPipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                 
                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "WWP" || type == "WWFM")
            {
                 var specific = InstalledAssets.WWPressurePipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.WWPressurePipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                 
                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "R")
            {
                 var specific = InstalledAssets.ReclaimedPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.ReclaimedPipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "G")
            {
                 var specific = InstalledAssets.GGravityPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.GGravityPipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "GP")
            {
                 var specific = InstalledAssets.GPressurePipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.GPressurePipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "E")
            {
                 var specific = InstalledAssets.EGravityPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.EGravityPipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "EP")
            {
                 var specific = InstalledAssets.EPressurePipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.EPressurePipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "ST" || type == "D")
            {
                 var specific = InstalledAssets.STGravityPipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.STGravityPipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.UpstreamPointId = run.FromPointId; item.DownstreamPointId = run.ToPointId;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);

                 assetToSave = item;
                 existing = specific;
            }
            else if (type == "STP" || type == "STFM")
            {
                 var specific = InstalledAssets.STPressurePipes.FirstOrDefault(x => HasScriptKey(x, key));
                 var item = specific ?? new RCS.Data.Entities.STPressurePipe();
                 
                 item.PartKey = run.PartKey; item.Size = run.Diameter.ToString(); item.Material = run.Material; 
                 
                 item.UpstreamInvert = run.InvertStart; item.DownstreamInvert = run.InvertEnd;
                 item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);

                 assetToSave = item;
                 existing = specific;
            }

            if (assetToSave != null)
            {
                assetToSave.UpdatedUtc = DateTime.UtcNow;
                if (existing == null)
                {
                    assetToSave.CreatedUtc = DateTime.UtcNow;
                    await InstalledAssets.AddItemAsync(assetToSave);
                    count++;
                }
                else
                {
                    await InstalledAssets.SaveItemAsync(assetToSave);
                    updated++;
                }
            }
        }

        var allPts = _context.GetAllPoints().ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);

        foreach (var s in result.Structures)
        {
            var p = _context.GetPoint(s.PointId);
            double n = p?.Northing ?? 0; double e = p?.Easting ?? 0; double z = p?.Elevation ?? 0;
            
            string desc = allPts.TryGetValue(s.PointId, out var pData) ? pData.Description : "";
            string t = $"{s.Type} {desc}".Trim().ToUpper();
            var tokens = t.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            string key = $"Pt-{s.PointId}";

            RCS.Data.Entities.InstalledAsset? existing = null;
            RCS.Data.Entities.InstalledAsset? assetToSave = null;
            
            bool isWater = tokens.Any(x => x.StartsWith("W") || x.Contains("WAT") || x == "FH" || x.StartsWith("JEAW") && !x.StartsWith("JEAWW"));
            bool isSewer = tokens.Any(x => x == "WW" || x == "WWM" || x == "WWV" || x.Contains("SAN") || x.Contains("SEW") || x.StartsWith("JEAWW"));
            bool isStorm = tokens.Any(x => x == "ST" || x.StartsWith("STM") || x == "S" || x == "D" || x.Contains("SW") || x.Contains("STORM") || x.StartsWith("JEAST"));
            bool isGas = tokens.Any(x => x.StartsWith("G") || x.Contains("GAS") || x.StartsWith("JEAG"));
            bool isElectric = tokens.Any(x => x.StartsWith("E") || x.Contains("ELEC") || x.Contains("PWR") || x.Contains("POLE") || x == "PP" || x == "GUY" || x.StartsWith("JEAE"));
            bool isReclaim = tokens.Any(x => x == "R" || x.Contains("RECLAIM") || x.StartsWith("JEAR"));

            // Note: If no group matches, it might skip creating an asset unless we define a fallback.
            if (isWater)
            {
                if (tokens.Any(x => x.Contains("MET") || x == "WMET")) 
                {
                     var specific = InstalledAssets.WaterMeters.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WaterMeter();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                }
                else if (tokens.Any(x => x.Contains("VALVE") || x.EndsWith("V") || x.EndsWith("VLV") || x == "WAR" || x.StartsWith("JEAWV"))) 
                {
                     var specific = InstalledAssets.WaterValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WaterValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                }
                else if (tokens.Any(x => x.Contains("HYDRANT") || x.EndsWith("H") || x.EndsWith("HYD") || x == "FH")) 
                {
                     var specific = InstalledAssets.WaterHydrants.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WaterHydrant();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                }
                else 
                {
                     var specific = InstalledAssets.WaterFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WaterFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                }
            }
            else if (isSewer)
            {
                if (tokens.Any(x => x.Contains("MH") || x.Contains("MANHOLE") || x == "WWM" || x.StartsWith("JEAWWM"))) 
                {
                     var specific = InstalledAssets.Manholes.FirstOrDefault(x => HasScriptKey(x, key)); // Using generic Manholes since no WWManhole exists specifically
                     var item = specific ?? new RCS.Data.Entities.Manhole();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                }
                else if (tokens.Any(x => x.Contains("VALVE") || x.EndsWith("V") || x.EndsWith("VLV") || x == "WWV" || x.StartsWith("JEAWWV"))) 
                {
                     var specific = InstalledAssets.WWValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WWValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                }
                else 
                {
                     var specific = InstalledAssets.WWFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.WWFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                }
            }
            else if (isReclaim)
            {
                 if (tokens.Any(x => x.Contains("MET") || x == "RMET")) 
                 {
                     var specific = InstalledAssets.ReclaimedMeters.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.ReclaimedMeter();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
                 else if (tokens.Any(x => x.Contains("VALVE") || x.EndsWith("V") || x.EndsWith("VLV"))) 
                 {
                     var specific = InstalledAssets.ReclaimedValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.ReclaimedValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
                 else if (tokens.Any(x => x.Contains("HYDRANT") || x.EndsWith("H") || x.EndsWith("HYD"))) 
                 {
                     var specific = InstalledAssets.ReclaimedHydrants.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.ReclaimedHydrant();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
                 else 
                 {
                     var specific = InstalledAssets.ReclaimedFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.ReclaimedFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
            }
            else if (isGas)
            {
                 if (tokens.Any(x => x.Contains("VALVE") || x.EndsWith("V") || x.EndsWith("VLV"))) 
                 {
                     var specific = InstalledAssets.GValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.GValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
                 else 
                 {
                     var specific = InstalledAssets.GFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.GFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
             }
             else if (isElectric)
             {
                 if (tokens.Any(x => x.Contains("VALVE") || x.EndsWith("V") || x.EndsWith("VLV"))) 
                 {
                     var specific = InstalledAssets.EValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.EValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
                 else 
                 {
                     // Treat poles/boxes as Fittings for now since EL doesn't have EPole in db? Wait. Does it? We can use EFitting. Let's just use EFitting.
                     var specific = InstalledAssets.EFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.EFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
             }
             else if (isStorm)
             {
                 if (tokens.Any(x => x.Contains("VALVE") || x.EndsWith("V") || x.EndsWith("VLV"))) 
                 {
                     var specific = InstalledAssets.STValves.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.STValve();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
                 else if (tokens.Any(x => x.Contains("MH") || x.Contains("MANHOLE") || x.Contains("CBI") || x.Contains("INLET") || x.Contains("BASIN") || x.Contains("STM")))
                 {
                     var specific = InstalledAssets.STManholes.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.STManhole();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
                 else 
                 {
                     var specific = InstalledAssets.STFittings.FirstOrDefault(x => HasScriptKey(x, key));
                     var item = specific ?? new RCS.Data.Entities.STFitting();
                     item.PartKey = s.Type; item.Northing = n; item.Easting = e; item.TopElevation = z;
                     item.SourceSheetRowIndex = AddScriptKey(item.SourceSheetRowIndex, key);
                     assetToSave = item; existing = specific;
                 }
             }

            if (assetToSave != null)
            {
                assetToSave.UpdatedUtc = DateTime.UtcNow;
                if (existing == null)
                {
                     assetToSave.CreatedUtc = DateTime.UtcNow;
                     await InstalledAssets.AddItemAsync(assetToSave);
                     count++;
                }
                else
                {
                    await InstalledAssets.SaveItemAsync(assetToSave);
                    updated++;
                }
            }
        }

        // --- Process Pipe Figures ---
        var allPtsDict = _context.GetAllPoints().ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);
        var runsByFig = result.Runs.GroupBy(r => r.FigureName).Where(g => !string.IsNullOrWhiteSpace(g.Key));
        foreach (var group in runsByFig)
        {
            string figName = group.Key;
            string figLayer = group.First().Type;
            
            var pts = new System.Collections.Generic.List<string>();
            foreach (var run in group)
            {
                if (pts.Count == 0 || pts.Last() != run.FromPointId)
                {
                    pts.Add(run.FromPointId);
                }
                pts.Add(run.ToPointId);
            }
            if (pts.Count < 2) continue;
            
            bool isClosed = pts.First() == pts.Last() && pts.Count > 2;
            
            var existingFig = InstalledAssets.FigureAssets.FirstOrDefault(f => string.Equals(f.Name, figName, StringComparison.OrdinalIgnoreCase));
            var newFig = existingFig ?? new RCS.Data.Entities.Figure 
            { 
                 Name = figName, 
                 Layer = figLayer,
                 PartKey = $"FIG-{Guid.NewGuid().ToString().Substring(0, 5)}",
                 ProjectId = _currentProject?.Id.ToString() ?? ""
            };
            
            var firstRun = group.First();
            newFig.Subtype = string.IsNullOrEmpty(newFig.Subtype) ? firstRun.Type : newFig.Subtype;
            newFig.Material = string.IsNullOrEmpty(newFig.Material) ? firstRun.Material : newFig.Material;
            newFig.Size = string.IsNullOrEmpty(newFig.Size) ? firstRun.Diameter.ToString() : newFig.Size;

            newFig.IsClosed = isClosed;
            newFig.UpdatedUtc = DateTime.UtcNow;
            newFig.Vertices.Clear();
            
            int vIdx = 0;
            foreach (var pid in pts)
            {
                if (allPtsDict.TryGetValue(pid, out var pData))
                {
                    var survPt = pData.Point;
                    newFig.Vertices.Add(new RCS.Data.Entities.FigureVertex 
                    {
                        PointId = $"{_currentProject?.Id.ToString() ?? ""}_{pid}",
                        OrderIndex = vIdx++
                    });
                }
            }
            
            if (existingFig == null)
            {
                newFig.CreatedUtc = DateTime.UtcNow;
                await InstalledAssets.AddItemAsync(newFig);
                count++;
            }
            else
            {
                await InstalledAssets.SaveItemAsync(newFig);
                updated++;
            }
        }

        // --- Process Pure COGO Figures ---
        foreach (var memFig in _context.GetAllFigures())
        {
            bool isClosed = memFig.PointIds.Count > 2 && memFig.PointIds.First() == memFig.PointIds.Last();
            if (isClosed)
            {
                var pts = new System.Collections.Generic.List<Point3D>();
                foreach (var id in memFig.PointIds)
                {
                    var p = _context.GetPoint(id);
                    if (p != null) pts.Add(p);
                }
                double areaSum = 0;
                for (int i = 0; i < pts.Count; i++)
                {
                    var p1 = pts[i];
                    var p2 = pts[(i + 1) % pts.Count];
                    areaSum += (p1.Easting * p2.Northing) - (p2.Easting * p1.Northing);
                }
                double area = Math.Abs(areaSum) * 0.5;
                if (area < MinimumBoundaryArea) 
                {
                    _context.Log($"[DEBUG] Figure '{memFig.Name}' area ({area}) < Minimum. Saving anyway.");
                    // continue; // Ignore parcels smaller than minimum
                }
            }

            var existingFig = InstalledAssets.FigureAssets.FirstOrDefault(f => string.Equals(f.Name, memFig.Name, StringComparison.OrdinalIgnoreCase));
            var newFig = existingFig ?? new RCS.Data.Entities.Figure 
            { 
                 Name = memFig.Name, 
                 Layer = "Geometry",
                 PartKey = $"FIG-{Guid.NewGuid().ToString().Substring(0, 5)}",
                 ProjectId = _currentProject?.Id.ToString() ?? ""
            };

            // Derive subtype automatically if it starts with known prefixes
            if (string.IsNullOrEmpty(newFig.Subtype))
            {
                var upperName = memFig.Name.ToUpper();
                if (upperName.Contains("LOT")) newFig.Subtype = "Lot";
                else if (upperName.Contains("BLDG") || upperName.Contains("BUILDING")) newFig.Subtype = "Building";
                else if (upperName.Contains("EDGE") || upperName.Contains("EOP") || upperName.Contains("EP") || upperName.Contains("ROAD")) newFig.Subtype = "Edge of Pavement";
                else newFig.Subtype = "Linework";
            }
            
            newFig.IsClosed = isClosed;
            newFig.UpdatedUtc = DateTime.UtcNow;
            
            newFig.Vertices.Clear();
            
            int vIdx = 0;
            foreach (var pid in memFig.PointIds)
            {
                if (allPtsDict.TryGetValue(pid, out var pData))
                {
                    var survPt = pData.Point;
                    newFig.Vertices.Add(new RCS.Data.Entities.FigureVertex 
                    {
                        PointId = $"{_currentProject?.Id.ToString() ?? ""}_{pid}",
                        OrderIndex = vIdx++
                    });
                }
            }

            if (existingFig == null)
            {
                newFig.CreatedUtc = DateTime.UtcNow;
                await InstalledAssets.AddItemAsync(newFig);
                count++;
            }
            else
            {
                await InstalledAssets.SaveItemAsync(newFig);
                updated++;
            }
        }

        _context.Log($"[AUDIT] Synced to Installed Assets: {count} Added, {updated} Updated.");
      }
      catch (Exception ex)
      {
           System.Windows.Application.Current?.Dispatcher.Invoke(() => {
               _context.Log($"[SYNC ERROR] Database synchronization failed: {ex.Message}");
               if (ex.InnerException != null) _context.Log($"[INNER DB ERROR] {ex.InnerException.Message}");
           });
      }
    }

    // --- Dropdown Sources ---
    public ObservableCollection<string> AvailableDisciplines { get; } = new();
    public ObservableCollection<string> AvailableFeatureTypes { get; } = new();
    public ObservableCollection<string> AvailableSizes { get; } = new();
    public ObservableCollection<string> AvailableMaterials { get; } = new();

    private string _filterDiscipline = "";
    public string FilterDiscipline { 
        get => _filterDiscipline; 
        set { SetField(ref _filterDiscipline, value); FilterCatalog(); } 
    }

    private string _filterFeatureType = "";
    public string FilterFeatureType { 
        get => _filterFeatureType; 
        set { SetField(ref _filterFeatureType, value); FilterCatalog(); } 
    }

    private string _filterSize = "";
    public string FilterSize { 
        get => _filterSize; 
        set { SetField(ref _filterSize, value); FilterCatalog(); } 
    }

    private string _filterMaterial = "";
    public string FilterMaterial { 
        get => _filterMaterial; 
        set { SetField(ref _filterMaterial, value); FilterCatalog(); } 
    }
    
    // We need a separate collection for the 'View' if we are filtering, 
    // OR we use a CollectionViewSource. ViewModel filtering is easier to control.
    public ObservableCollection<RCS.Piping.Core.Models.MaterialItem> FilteredCatalog { get; } = new();

    private void PopulateDropdowns()
    {
        AvailableDisciplines.Clear();
        AvailableDisciplines.Add(""); // All
        foreach (var x in MasterCatalog.Select(x => x.Discipline).Distinct().OrderBy(x => x)) AvailableDisciplines.Add(x);

        AvailableFeatureTypes.Clear();
        AvailableFeatureTypes.Add("");
        foreach (var x in MasterCatalog.Select(x => x.FeatureType).Distinct().OrderBy(x => x)) AvailableFeatureTypes.Add(x);

        AvailableSizes.Clear();
        AvailableSizes.Add("");
        foreach (var x in MasterCatalog.Select(x => x.Size).Distinct().OrderBy(x => x)) AvailableSizes.Add(x);

        AvailableMaterials.Clear();
        AvailableMaterials.Add("");
        foreach (var x in MasterCatalog.Select(x => x.Material).Distinct().OrderBy(x => x)) AvailableMaterials.Add(x);
        
        FilterCatalog();
    }
    
    private void FilterCatalog()
    {
        var query = MasterCatalog.AsEnumerable();
        
        if (!string.IsNullOrEmpty(FilterDiscipline)) query = query.Where(x => x.Discipline == FilterDiscipline);
        if (!string.IsNullOrEmpty(FilterFeatureType)) query = query.Where(x => x.FeatureType == FilterFeatureType);
        if (!string.IsNullOrEmpty(FilterSize)) query = query.Where(x => x.Size == FilterSize);
        if (!string.IsNullOrEmpty(FilterMaterial)) query = query.Where(x => x.Material == FilterMaterial);
        
        FilteredCatalog.Clear();
        foreach(var item in query) FilteredCatalog.Add(item);
    }

}
