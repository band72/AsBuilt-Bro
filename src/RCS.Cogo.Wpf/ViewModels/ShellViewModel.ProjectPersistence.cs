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
    private bool ConfirmDiscardChanges()
    {
        if (!IsDirty || CurrentProject == null) return true;

        string name = string.IsNullOrWhiteSpace(CurrentProject.ProjectName)
            ? "Untitled Project" : CurrentProject.ProjectName;

        var result = System.Windows.MessageBox.Show(
            $"Save changes to '{name}' before continuing?",
            "Unsaved Changes",
            System.Windows.MessageBoxButton.YesNoCancel,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Cancel) return false;
        if (result == System.Windows.MessageBoxResult.Yes)  SaveProject();
        return true;
    }

    private void ResetProjectState()
    {
        CurrentProject  = new Project();
        _currentDbPath  = string.Empty;
        IsDirty         = false;
        _context.ClearState();
        _pipeNetwork    = new RCS.Piping.Core.Models.PipeNetwork();
        _pipelineRunner = new RCS.Piping.Core.Runner.PipelineRunner(_context, _pipeNetwork);
        PipeRuns.Clear();
        Structures.Clear();
        StructureGraphics.Clear();
        Points.Clear();
        Figures.Clear();
        RefreshData();
    }

    private void NewProject(bool skipEdit = false)
    {
        if (!ConfirmDiscardChanges()) return;

        ResetProjectState();
        _context.Log("[AUDIT] Created New Project");

        if (!skipEdit)
        {
            var window = new RCS.Cogo.Wpf.Views.ProjectDetailsWindow(CurrentProject);
            if (window.ShowDialog() == true)
            {
                _context.Log($"[AUDIT] Updated Project Details: {CurrentProject.ProjectName}");
                
                // Automatically save the physical DB file into the generated SaveLocation in one step
                if (!string.IsNullOrWhiteSpace(CurrentProject.SaveLocation))
                {
                    string safeName = string.IsNullOrWhiteSpace(CurrentProject.ProjectName)
                        ? "Untitled_Project"
                        : string.Concat(CurrentProject.ProjectName.Split(System.IO.Path.GetInvalidFileNameChars()));
                        
                    string fullDbPath = System.IO.Path.Combine(CurrentProject.SaveLocation, $"{safeName}.db");
                    SaveProjectInternal(fullDbPath);
                }
            }
        }

        IsDirty = false; // brand-new project is not dirty until user makes changes
    }

    private void EditProject()
    {
        var window = new RCS.Cogo.Wpf.Views.ProjectDetailsWindow(CurrentProject);
        if (window.ShowDialog() == true)
        {
            _context.Log($"[AUDIT] Updated Project Details: {CurrentProject.ProjectName}");
            // Refresh title or status bar if bound?
            // For now, logging confirms update.
        }
    }

    public System.Windows.Input.ICommand OpenReportSettingsCommand { get; }
    public System.Windows.Input.ICommand OpenValidationSettingsCommand { get; }
    public System.Windows.Input.ICommand OpenGeneralSettingsCommand { get; }

    private void OpenGeneralSettings()
    {
        var window = new RCS.Cogo.Wpf.Views.GeneralSettingsWindow
        {
            DataContext = this
        };
        window.ShowDialog();
    }

    private void OpenAlignmentSettings()
    {
        var window = new RCS.Cogo.Wpf.Views.AlignmentSettingsWindow(this);
        window.ShowDialog();
    }

    private void OpenPipeCharacteristics()
    {
        var window = new RCS.Cogo.Wpf.Views.PipeCharacteristicsWindow();
        window.ShowDialog();
    }

    private void OpenValidationSettings()
    {
        var window = new RCS.Cogo.Wpf.Views.ValidationSettingsWindow();
        window.ShowDialog();
        _context.Log("[AUDIT] Validation Settings Window Closed.");
    }

    private void OpenReportSettings()
    {
        var window = new RCS.Cogo.Wpf.Views.ReportSettingsWindow(CurrentProject.ReportConfig);
        if (window.ShowDialog() == true)
        {
            _context.Log($"[AUDIT] Updated Report Settings for {CurrentProject.ProjectName}");
        }
    }

    private void SaveProject()
    {
        if (string.IsNullOrWhiteSpace(_currentDbPath))
            SaveProjectAs();
        else
            SaveProjectInternal(_currentDbPath);
    }

    private void SaveProjectAs()
    {
        string safeName = string.IsNullOrWhiteSpace(CurrentProject?.ProjectName)
            ? "Untitled"
            : string.Concat(CurrentProject.ProjectName.Split(System.IO.Path.GetInvalidFileNameChars()));

        string initialDir = !string.IsNullOrWhiteSpace(_currentDbPath)
            ? System.IO.Path.GetDirectoryName(_currentDbPath) ?? string.Empty
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title            = "Save RCS Project As",
            Filter           = "RCS Project (*.db)|*.db|All Files (*.*)|*.*",
            DefaultExt       = ".db",
            FileName         = $"{safeName.Replace(' ', '_')}.db",
            InitialDirectory = initialDir
        };

        if (dialog.ShowDialog() == true)
            SaveProjectInternal(dialog.FileName);
    }

    private void SaveProjectInternal(string filePath)
    {
        try 
        {
            // Sync Data to Project Model
            CurrentProject.Points = _context.GetAllPoints().Select(p => new PointEntry 
            {
                Id = p.Id,
                Northing = p.Point.Northing,
                Easting = p.Point.Easting,
                Elevation = p.Point.Elevation,
                Description = p.Description
            }).ToList();

            CurrentProject.PipeRuns = _pipeNetwork.Runs.Values.ToList();
            CurrentProject.Structures = _pipeNetwork.Structures.Values.ToList();
            CurrentProject.Materials = ProjectMaterials.ToList();

            var service = new LiteDbProjectService();
            service.SaveProject(filePath, CurrentProject);
            _currentDbPath = filePath; // Store path for maintenance and point syncing operations
            
            // Method 1: Push points to Survey Linework Entity Framework DB for Figures foreign keys
            try 
            {
                using (var db = new RCS.Data.AppDbContext())
                {
                    var newSurveyPoints = CurrentProject.Points.Select(p => new RCS.Data.Entities.SurveyPoint 
                    {
                        Id = $"{CurrentProject.Id}_{p.Id}", 
                        PointNumber = p.Id,
                        ProjectId = CurrentProject.Id.ToString(), 
                        Northing = p.Northing, Easting = p.Easting, Elevation = p.Elevation, Description = p.Description 
                    }).ToList();

                    var existing = db.SurveyPoints.Where(p => p.ProjectId == CurrentProject.Id.ToString()).Select(p => p.Id).ToList();
                    var toInsert = newSurveyPoints.Where(p => !existing.Contains(p.Id)).ToList();
                    
                    // We also need to update existing ones if coordinates changed to be totally robust
                    var toUpdateIds = newSurveyPoints.Where(p => existing.Contains(p.Id)).ToList();
                    foreach(var upPt in toUpdateIds)
                    {
                        var tracking = db.SurveyPoints.Local.FirstOrDefault(x => x.Id == upPt.Id) ?? db.SurveyPoints.FirstOrDefault(x => x.Id == upPt.Id);
                        if (tracking != null)
                        {
                            tracking.Northing = upPt.Northing;
                            tracking.Easting = upPt.Easting;
                            tracking.Elevation = upPt.Elevation;
                            tracking.Description = upPt.Description;
                        }
                    }

                    if (toInsert.Any() || toUpdateIds.Any())
                    {
                        db.SurveyPoints.AddRange(toInsert);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception dbEx)
            {
                string inner = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                _context.Log($"[AUDIT] Warning: Could not sync points with Method 1 SQLite DB: {dbEx.Message}. Inner: {inner}");
            }

            _context.Log($"[AUDIT] Saved project to {filePath} (LiteDB and EF Core DB)");
            
            IsDirty = false;
            UpdateWindowTitle();
        }
        catch(Exception ex)
        {
            _context.Log($"[AUDIT] Error Saving Project: {ex.Message}");
            System.Windows.MessageBox.Show($"Error Saving Project: {ex.Message}");
        }
    }

    private string _currentDbPath = string.Empty; // Store path for maintenance operations

    /// <summary>Open-project logic shared by OpenProject() and OpenRecentFile().</summary>
    private void LoadProjectFromPath(string filePath)
    {
        var loader = new RCS.Cogo.Wpf.Views.LoadingWindow($"Loading {System.IO.Path.GetFileName(filePath)}…", async () =>
        {
            try
            {
                // 1. Reset state AFTER confirming — we already confirmed in the caller
                System.Windows.Application.Current.Dispatcher.Invoke(() => ResetProjectState());

                var service       = new LiteDbProjectService();
                var loadedProject = await Task.Run(() => service.LoadProject(filePath));

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentProject = loadedProject;
                    _currentDbPath = filePath;
                    IsDirty        = false;
                    PushRecentFile(filePath, loadedProject.ProjectName);

                    // Repopulate Context
                    if (CurrentProject.Points != null)
                        foreach (var p in CurrentProject.Points)
                            _context.AddPoint(p.Id, new Point3D(p.Northing, p.Easting, p.Elevation), p.Description);

                    // Repopulate Piping
                    PipeRuns.Clear();
                    Structures.Clear();

                    if (CurrentProject.PipeRuns != null)
                        foreach (var run in CurrentProject.PipeRuns) { _pipeNetwork.AddRun(run); PipeRuns.Add(run); }

                    if (CurrentProject.Structures != null)
                        foreach (var s in CurrentProject.Structures) { _pipeNetwork.AddStructure(s); Structures.Add(s); }

                    if (CurrentProject.Materials != null)
                    { ProjectMaterials.Clear(); foreach (var m in CurrentProject.Materials) ProjectMaterials.Add(m); }

                    // Rehydrate figures from SQLite
                    var projIdStr = _currentProject?.Id.ToString() ?? "";
                    try
                    {
                        using var figDb = new RCS.Data.AppDbContext();
                        var dbFigures = figDb.Set<RCS.Data.Entities.Figure>()
                            .Include("Vertices.Point")
                            .Where(f => f.ProjectId == projIdStr)
                            .ToList();

                        foreach (var dbFig in dbFigures)
                        {
                            if (dbFig.Vertices == null || dbFig.Vertices.Count == 0) continue;

                            var orderedIds = new System.Collections.Generic.List<string>();
                            bool anyMissing = false;
                            foreach (var v in dbFig.Vertices.OrderBy(v => v.OrderIndex))
                            {
                                var rawPid = v.PointId;
                                var prefix = projIdStr + "_";
                                if (!string.IsNullOrEmpty(prefix) && rawPid.StartsWith(prefix))
                                    rawPid = rawPid.Substring(prefix.Length);

                                if (!_context.PointExists(rawPid))
                                {
                                    if (v.Point != null)
                                        _context.AddPoint(rawPid, new Point3D(v.Point.Northing, v.Point.Easting, v.Point.Elevation), "");
                                    else { anyMissing = true; break; }
                                }
                                orderedIds.Add(rawPid);
                            }
                            if (anyMissing || orderedIds.Count < 2) continue;

                            // Adaptive crosslink split
                            var segDists = new System.Collections.Generic.List<double>();
                            for (int i = 0; i < orderedIds.Count - 1; i++)
                            {
                                var pA = _context.GetPoint(orderedIds[i]);
                                var pB = _context.GetPoint(orderedIds[i + 1]);
                                segDists.Add((pA != null && pB != null)
                                    ? Math.Sqrt(Math.Pow(pB.Easting - pA.Easting, 2) + Math.Pow(pB.Northing - pA.Northing, 2))
                                    : 0);
                            }
                            var sorted2 = new System.Collections.Generic.List<double>(segDists); sorted2.Sort();
                            double medianDist = sorted2[sorted2.Count / 2];
                            double crosslinkCutoff = Math.Max(medianDist * 2.0, 110.0);

                            string baseName = dbFig.Name;
                            int segIdx = 1;
                            var currentSegIds = new System.Collections.Generic.List<string> { orderedIds[0] };

                            for (int i = 0; i < orderedIds.Count - 1; i++)
                            {
                                double dist = segDists[i];
                                if (dist > crosslinkCutoff)
                                {
                                    if (currentSegIds.Count > 1)
                                    {
                                        string segName = segIdx == 1 ? baseName : $"{baseName}_{segIdx}";
                                        var seg = new RCS.Cogo.App.State.Figure(segName);
                                        foreach (var id in currentSegIds) seg.PointIds.Add(id);
                                        _context.AddFigure(seg);
                                        _context.Log($"[LOAD] Split '{baseName}' → '{segName}' ({currentSegIds.Count} pts). Outlier seg: {dist:F0} ft.");
                                    }
                                    segIdx++;
                                    currentSegIds = new System.Collections.Generic.List<string>();
                                }
                                currentSegIds.Add(orderedIds[i + 1]);
                            }
                            if (currentSegIds.Count > 1)
                            {
                                string segName = segIdx == 1 ? baseName : $"{baseName}_{segIdx}";
                                var seg = new RCS.Cogo.App.State.Figure(segName);
                                foreach (var id in currentSegIds) seg.PointIds.Add(id);
                                _context.AddFigure(seg);
                            }
                        }
                        _context.Log($"[AUDIT] Rehydrated {_context.GetAllFigures().Count()} figures from database.");
                    }
                    catch (Exception figEx) { _context.Log($"[WARN] Could not rehydrate figures: {figEx.Message}"); }

                    _context.Log("[AUDIT] Opened Project (loading alignments...)");
                }); // end Dispatcher.Invoke

                // Rehydrate Alignments & Profiles (must run in async scope, not inside Dispatcher.Invoke)
                var projIdOuter = _currentProject?.Id.ToString() ?? "";
                try
                {
                    using var algnDb = new RCS.Data.AppDbContext();
                    var algnFigs = algnDb.Set<RCS.Data.Entities.Figure>()
                        .Where(f => f.ProjectId == projIdOuter &&
                                   (f.Layer == "Horizontal_Align" || f.Layer == "Vertical_Align"))
                        .OrderBy(f => f.Layer)
                        .ToList();

                    int rehydratedAlgns = 0;
                    foreach (var af in algnFigs)
                    {
                        if (string.IsNullOrWhiteSpace(af.ScriptContent)) continue;
                        var algnLines = af.ScriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                        var pointCmds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            { "PT", "PNT", "NE", "NEZ", "ST" };
                        foreach (var aln in algnLines)
                        {
                            var t = aln.Trim();
                            if (string.IsNullOrWhiteSpace(t) || t.StartsWith("//")) continue;
                            var cmd = t.Split(' ')[0].ToUpperInvariant();
                            if (pointCmds.Contains(cmd)) try { await _engine.ExecuteAsync(t, _context); } catch { }
                        }
                        foreach (var aln in algnLines)
                        {
                            var t = aln.Trim();
                            if (string.IsNullOrWhiteSpace(t) || t.StartsWith("//")) continue;
                            var cmd = t.Split(' ')[0].ToUpperInvariant();
                            if (cmd == "ALGN" || cmd == "PROF" || cmd == "VPI")
                                try { await _engine.ExecuteAsync(t, _context); } catch { }
                        }
                        rehydratedAlgns++;
                    }
                    if (rehydratedAlgns > 0)
                        _context.Log($"[AUDIT] Rehydrated {rehydratedAlgns} alignment/profile script(s).");
                }
                catch (Exception algnEx) { _context.Log($"[WARN] Could not rehydrate alignments: {algnEx.Message}"); }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RefreshData();
                    // Reload asset tables so previously imported JEA data appears in the viewer
                    _ = LoadInstalledAssetsAsync();
                    
                    if (CurrentProject?.SavedViewMatrix != null && CurrentProject.SavedViewMatrix.Length == 6)
                    {
                        ViewRestoreRequested?.Invoke(this, CurrentProject.SavedViewMatrix);
                    }
                    else
                    {
                        ZoomExtentsRequested?.Invoke(this, EventArgs.Empty);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _context.Log($"[AUDIT] Error Opening Project: {ex.Message}");
                    System.Windows.MessageBox.Show($"Error Opening Project: {ex.Message}");
                });
            }
        });

        loader.ShowDialog();
    }

    private void OpenProject()
    {
        if (!ConfirmDiscardChanges()) return;

        string initialDir = !string.IsNullOrWhiteSpace(_currentDbPath)
            ? System.IO.Path.GetDirectoryName(_currentDbPath) ?? string.Empty
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title            = "Open RCS Project",
            Filter           = "RCS Project (*.db)|*.db|All Files (*.*)|*.*",
            InitialDirectory = initialDir
        };

        if (dialog.ShowDialog() == true)
            LoadProjectFromPath(dialog.FileName);
    }

    private void CloseProject()
    {
        if (!ConfirmDiscardChanges()) return;
        ResetProjectState();
        _context.Log("[AUDIT] Closed Project");
    }
    // --- Database Maintenance ---
    public System.Windows.Input.ICommand CompactDbCommand { get; }
    public System.Windows.Input.ICommand VerifyDbCommand { get; }
    public System.Windows.Input.ICommand RepairDbCommand { get; }
    public System.Windows.Input.ICommand ExportDbCsvCommand { get; }

    private void CompactDatabase()
    {
        try 
        {
            using (var db = new RCS.Data.AppDbContext())
            {
                db.Database.ExecuteSqlRaw("VACUUM;");
            }
            _context.Log("[AUDIT] System Database Compacted Successfully (VACUUM).");
            System.Windows.MessageBox.Show("System Database Compacted Successfully.");
        }
        catch(Exception ex)
        {
            _context.Log($"[AUDIT] Error Compacting DB: {ex.Message}");
        }
    }

    private void VerifyDatabase()
    {
        try 
        {
            string status = "Unknown";
            using (var db = new RCS.Data.AppDbContext())
            {
                var conn = db.Database.GetDbConnection();
                conn.Open();
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "PRAGMA integrity_check;";
                    status = command.ExecuteScalar()?.ToString() ?? "Unknown";
                }
                conn.Close();
            }
            
            bool ok = status?.Contains("ok", StringComparison.OrdinalIgnoreCase) == true;
            string msg = ok ? "System Database Verification Passed (Integrity OK)." : $"System Database Verification FAILED: {status}";
            _context.Log($"[AUDIT] {msg}");
            System.Windows.MessageBox.Show(ok ? "Verification Passed: Integrity OK" : $"Verification Failed: {status}");
        }
        catch(Exception ex)
        {
            _context.Log($"[AUDIT] Error Verifying DB: {ex.Message}");
        }
    }

    private void RepairDatabase()
    {
        try
        {
            // SQLite doesn't easily 'repair' in-place via simple commands aside from VACUUM/REINDEX. 
            using (var db = new RCS.Data.AppDbContext())
            {
                db.Database.ExecuteSqlRaw("REINDEX; VACUUM;");
            }
            _context.Log("[AUDIT] System Database Repair Sequence completed (REINDEX/VACUUM).");
            System.Windows.MessageBox.Show("System Database Repair Sequence Completed.");
        }
        catch (Exception ex)
        {
             _context.Log($"[AUDIT] Error Reparing DB: {ex.Message}");
        }
    }

    private void ExportDatabaseCsv()
    {
         var dialog = new Microsoft.Win32.SaveFileDialog
         {
             Filter = "CSV File (*.csv)|*.csv",
             FileName = "ProjectPoints_Export.csv"
         };

         if (dialog.ShowDialog() == true)
         {
             try
             {
                 var sb = new System.Text.StringBuilder();
                 sb.AppendLine("PointID,Northing,Easting,Elevation,Description");
                 foreach(var p in Points)
                 {
                     sb.AppendLine($"{p.Id},{p.Northing},{p.Easting},{p.Elevation},{p.Description}");
                 }
                 File.WriteAllText(dialog.FileName, sb.ToString());
                 _context.Log($"[AUDIT] Exported Database Points to {dialog.FileName}");
             }
             catch(Exception ex)
             {
                 _context.Log($"[AUDIT] Export Error: {ex.Message}");
             }
         }
    }

    public System.Windows.Input.ICommand ExportInstalledAssetsCommand { get; }

    private void ExportInstalledAssets()
    {
         var dialog = new Microsoft.Win32.SaveFileDialog
         {
             Title = "Export Installed Assets Report (Base Filename)",
             Filter = "CSV File (*.csv)|*.csv",
             FileName = "InstalledAssets_Report.csv"
         };

         if (dialog.ShowDialog() == true)
         {
             try
             {
                 InstalledAssets.ExportToFolder(System.IO.Path.GetDirectoryName(dialog.FileName) ?? "", "csv");
                 _context.Log($"[AUDIT] Exported Installed Assets to {System.IO.Path.GetDirectoryName(dialog.FileName)}");
             }
             catch(Exception ex)
             {
                 _context.Log($"[AUDIT] Export Error: {ex.Message}");
             }
         }
    }

    // ── JEA As-Built Template Export + Validation ────────────────────────
    public System.Windows.Input.ICommand ExportJeaTemplateCommand { get; }
    public System.Windows.Input.ICommand ValidateJeaCommand       { get; }
    public System.Windows.Input.ICommand ExportJeaMixScriptCommand { get; }
    public System.Windows.Input.ICommand GenerateJeaLineworkCommand { get; }

    private void OpenJeaValidation()
    {
        if (_currentProject == null)
        {
            System.Windows.MessageBox.Show("Please open a project first.",
                "No Project", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        string projectId = _currentProject.Id.ToString();
        var win = new RCS.Cogo.Wpf.Views.JeaValidationWindow(
            projectId,
            RCS.Cogo.Wpf.Views.JeaValidationMode.Standalone)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        // Auto-run so the user sees results immediately on open.
        try
        {
            var report = RCS.Cogo.Wpf.Services.JeaValidationService.Validate(projectId);
            win.LoadReport(report);
            JeaValidationIssueCount = report.Issues.Count;
            JeaValidationErrorCount = report.ErrorCount;
        }
        catch (Exception vex)
        {
            _context.Log($"[JEA] Pre-validation error: {vex.Message}");
        }

        win.ShowDialog();

        // Re-read in case user re-ran validation inside the window.
        JeaValidationIssueCount = win.TotalIssueCount;
        JeaValidationErrorCount = win.ErrorCount;

        _context.Log(JeaValidationIssueCount == 0
            ? "[JEA] Validation passed — no issues found."
            : $"[JEA] Validation complete — {JeaValidationErrorCount} error(s), " +
              $"{JeaValidationIssueCount - JeaValidationErrorCount} warning(s).");
    }

    private string BuildJeaMixScript(string projectIdStr)
    {
        using var db = new RCS.Data.AppDbContext();
        var waterFittings = db.WaterFittings.Where(x => x.ProjectId == projectIdStr).OrderBy(x => x.PartKey).ToList();
        var manholes = db.Manholes.Where(x => x.ProjectId == projectIdStr).OrderBy(x => x.PartKey).ToList();
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("// ==========================================");
        sb.AppendLine("// JEA AS-BUILT REVERSE ENGINEERED MIX SCRIPT");
        sb.AppendLine("// ==========================================");
        sb.AppendLine();
        sb.AppendLine("// 1. Establish Structural Node Points");
        
        foreach(var mh in manholes)
        {
            var id = string.IsNullOrWhiteSpace(mh.PartKey) ? mh.Id.Substring(0, 5) : mh.PartKey;
            var elev = mh.RimElevation ?? 0.0;
            var n = mh.Northing ?? 0.0;
            var e = mh.Easting ?? 0.0;
            sb.AppendLine($"ST {id.Replace(" ", "-")} {n:F4} {e:F4} {elev:F2} \"{mh.Subtype} {mh.Size} IN\"");
        }

        foreach(var wf in waterFittings)
        {
            var id = string.IsNullOrWhiteSpace(wf.PartKey) ? wf.Id.Substring(0, 5) : wf.PartKey;
            var elev = wf.TopElevation ?? 0.0;
            var n = wf.Northing ?? 0.0;
            var e = wf.Easting ?? 0.0;
            sb.AppendLine($"ST {id.Replace(" ", "-")} {n:F4} {e:F4} {elev:F2} \"{wf.Subtype} {wf.Size} IN\"");
        }

        sb.AppendLine();
        sb.AppendLine("// 2. Map Dynamic Transmission Lines");
        int added = 0;
        string prevPoint = "";
        double prevElev = 0.0;
        
        foreach(var wf in waterFittings)
        {
            var pt = string.IsNullOrWhiteSpace(wf.PartKey) ? wf.Id.Substring(0, 5) : wf.PartKey;
            var el = wf.TopElevation ?? 0.0;
            if (added > 0 && !string.IsNullOrEmpty(prevPoint))
            {
                    string sz = string.IsNullOrWhiteSpace(wf.Size) ? "8" : wf.Size;
                    sb.AppendLine($"PRUN START {prevPoint.Replace(" ", "-")} {pt.Replace(" ", "-")} {sz} {prevElev:F2} {el:F2}");
            }
            prevPoint = pt;
            prevElev = el;
            added++;
        }

        sb.AppendLine();
        sb.AppendLine("ECHO \"Mixed-Mode JEA Network Import Complete\"");
        return sb.ToString();
    }

    private void GenerateJeaLinework()
    {
        if (_currentProject == null) return;
        try
        {
            var script = BuildJeaMixScript(_currentProject.Id.ToString());
            
            // Append to existing piping script or set it
            if (string.IsNullOrWhiteSpace(PipingScriptText))
                PipingScriptText = script;
            else
                PipingScriptText = PipingScriptText + "\r\n\r\n" + script;

            _context.Log("[JEA] Automatically built Piping Mix Script from SQL DB. Compiling...");
            
            // Execute the script so linework appears immediately
            ProcessPipingScript();
            
            // Bring focus to structures view
            SelectedTabIndex = 2; // Typically 2 = piping tab, or view structures, depends on setup
            ZoomExtentsRequested?.Invoke(this, EventArgs.Empty);
        }
        catch(Exception ex)
        {
            _context.Log($"[JEA] Auto-Generate Linework Error: {ex.Message}");
        }
    }

    private void ExportJeaMixScript()
    {
        if (_currentProject == null)
        {
            System.Windows.MessageBox.Show("Please open a project first.",
                "No Project", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export JEA As-Built Cogo Mix Script",
            Filter = "Text File (*.txt)|*.txt",
            FileName = "JEA_Mix_Script_" + DateTime.Now.ToString("yyyyMMdd") + ".txt"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                string script = BuildJeaMixScript(_currentProject.Id.ToString());
                System.IO.File.WriteAllText(dialog.FileName, script);
                _context.Log($"[AUDIT] Exported JEA Mix Script to: {dialog.FileName}");
                
                System.Windows.MessageBox.Show("Script exported successfully!\nYou can copy and paste this file directly into the piping scripts window.", "Export Complete", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch(Exception ex)
            {
                _context.Log($"[AUDIT] Mix Script Export Error: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to export script: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void ExportJeaTemplate()
    {
        if (_currentProject == null)
        {
            System.Windows.MessageBox.Show("Please open a project first.",
                "No Project", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        // Open validator first — it has a "Proceed to Export" button that
        // calls back into the actual export logic
        var validationWin = new RCS.Cogo.Wpf.Views.JeaValidationWindow(
            _currentProject.Id.ToString(),
            RCS.Cogo.Wpf.Views.JeaValidationMode.Export,
            onProceedAction: () => RunJeaExport());
        validationWin.Owner = System.Windows.Application.Current.MainWindow;
        var result = validationWin.ShowDialog();

        // If user clicked "Proceed" the callback already ran; skip if cancelled
    }

    private void RunJeaExport()
    {
        if (_currentProject == null) return;

        // Use saved template path if available, otherwise browse
        string templatePath = JeaTemplatePath;
        if (string.IsNullOrWhiteSpace(templatePath) || !System.IO.File.Exists(templatePath))
        {
            var templateDlg = new Microsoft.Win32.OpenFileDialog
            {
                Title    = "Select Blank JEA As-Built Template (.xlsx)",
                Filter   = "Excel Workbook|*.xlsx",
            FileName = "JEA As Built Template 2024.xlsx"
            };
            if (templateDlg.ShowDialog() != true) return;
            templatePath = templateDlg.FileName;
            // Auto-save so user won't be asked again
            JeaTemplatePath = templatePath;
            RCS.Services.GlobalSettingsService.SaveSetting("JeaTemplatePath", templatePath);
        }

        // Step 2: choose output location
        var projectId   = _currentProject.Id.ToString();
        var projectName = string.IsNullOrWhiteSpace(_currentProject.ProjectName)
            ? "JEA_AsBuilt" : _currentProject.ProjectName;
        var safeName = string.Join("_",
            projectName.Split(System.IO.Path.GetInvalidFileNameChars()));

        var saveDlg = new Microsoft.Win32.SaveFileDialog
        {
            Title    = "Save Filled JEA Template As",
            Filter   = "Excel Workbook|*.xlsx",
            FileName = $"{safeName}_AsBuilt_{DateTime.Now:yyyyMMdd}.xlsx",
            InitialDirectory = string.IsNullOrWhiteSpace(_currentProject.SaveLocation)
                ? System.IO.Path.GetDirectoryName(templatePath) ?? ""
                : _currentProject.SaveLocation
        };
        if (saveDlg.ShowDialog() != true) return;

        // Step 3: run export
        try
        {
            _context.Log("[JEA] Starting JEA As-Built export...");
            var result = RCS.Cogo.Wpf.Services.JeaExportService.Export(
                templatePath,
                saveDlg.FileName,
                projectId,
                projectName);

            if (result.Success)
            {
                _context.Log(result.Summary());
                _context.Log($"[JEA] Export complete → {saveDlg.FileName}");

                var msg = $"JEA As-Built export complete!\n\n" +
                          $"Total rows written: {result.TotalRows}\n" +
                          $"Saved to:\n{saveDlg.FileName}\n\n" +
                          $"Open the file now?";

                var open = System.Windows.MessageBox.Show(msg, "Export Complete",
                    System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);

                if (open == System.Windows.MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName  = saveDlg.FileName,
                        UseShellExecute = true
                    });
            }
            else
            {
                _context.Log($"[JEA] Export failed: {result.ErrorMessage}");
                System.Windows.MessageBox.Show(
                    $"Export failed:\n{result.ErrorMessage}", "Export Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _context.Log($"[JEA] Export exception: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Export failed:\n{ex.Message}", "Export Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    // ── JEA Excel Import ──────────────────────────────────────────────────────
    public System.Windows.Input.ICommand ImportJeaTemplateCommand { get; }
    public System.Windows.Input.ICommand ImportS1AProjectCommand { get; }

    private void ImportJeaFromTemplate()
    {
        if (_currentProject == null)
        {
            System.Windows.MessageBox.Show("Please open a project first.",
                "No Project", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Select Filled JEA As-Built Excel File to Import",
            Filter = "Excel Workbook|*.xlsx",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            _context.Log($"[JEA] Importing from: {dlg.FileName}");
            var result = RCS.Cogo.Wpf.Services.JeaImportService.Import(
                dlg.FileName, _currentProject.Id.ToString());

            if (result.Success)
            {
                _context.Log("╔══════════════════════════════════════════════");
                _context.Log("║  JEA Import Summary");
                _context.Log("╠══════════════════════════════════════════════");
                foreach (var s in result.Sheets.Where(sh => sh.Imported > 0 || sh.Warnings.Count > 0))
                {
                    _context.Log($"║  {s.SheetName,-38}: {s.Imported,4} rows  ({s.Skipped} skipped)");
                    foreach (var w in s.Warnings.Take(5))
                        _context.Log($"║     ⚠ {w}");
                }
                _context.Log("╠══════════════════════════════════════════════");
                _context.Log($"║  TOTAL IMPORTED : {result.TotalImported,4}");
                _context.Log($"║  TOTAL SKIPPED  : {result.TotalSkipped,4}");
                _context.Log("╚══════════════════════════════════════════════");

                RefreshData(true);

                // Reload the InstalledAssets ViewModel so the tables viewer reflects the new data.
                // Without this, the viewer collections stay empty even though the DB is populated.
                _ = LoadInstalledAssetsAsync();

                var validationWin = new RCS.Cogo.Wpf.Views.JeaValidationWindow(
                    _currentProject.Id.ToString(),
                    RCS.Cogo.Wpf.Views.JeaValidationMode.Import,
                    onProceedAction: () => 
                    {
                        var tablesWin = new RCS.Cogo.Wpf.Views.InstalledAssetsTablesWindow(_currentProject.Id.ToString());
                        tablesWin.Owner = System.Windows.Application.Current.MainWindow;
                        tablesWin.Show();
                    });
                validationWin.Owner = System.Windows.Application.Current.MainWindow;
                validationWin.ShowDialog();
            }
            else
            {
                _context.Log($"[JEA] Import failed: {result.ErrorMessage}");
                System.Windows.MessageBox.Show(
                    $"Import failed:\n{result.ErrorMessage}", "Import Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _context.Log($"[JEA] Import exception: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Import failed:\n{ex.Message}", "Import Error",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }


    /// <summary>
    /// Creates a new project named "70498-S1A" and immediately imports all tables
    /// from Segment1A_All_Tables_Database_Ready.xlsx into the system database.
    /// The .db file is saved next to the Excel workbook.
    /// </summary>
    private void ImportS1AProjectFromExcel()
    {
        // 1. Let user confirm or browse for the S1A Excel file
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Select Segment1A_All_Tables_Database_Ready.xlsx",
            Filter = "Excel Workbook|*.xlsx",
            FileName = "Segment1A_All_Tables_Database_Ready.xlsx",
            InitialDirectory = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads")
        };
        if (dlg.ShowDialog() != true) return;

        string xlsxPath = dlg.FileName;
        string folder   = System.IO.Path.GetDirectoryName(xlsxPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string dbPath   = System.IO.Path.Combine(folder, "70498-S1A.db");

        // 2. Create / reset a fresh project
        if (!ConfirmDiscardChanges()) return;
        ResetProjectState();

        CurrentProject.ProjectName  = "70498-S1A";
        CurrentProject.AvailNo      = "70498";
        CurrentProject.Utility      = "Water";
        CurrentProject.Units        = "USFT";
        CurrentProject.SaveLocation = folder;

        // 3. Persist the project .db
        System.IO.Directory.CreateDirectory(folder);
        SaveProjectInternal(dbPath);
        _context.Log($"[S1A] New project '70498-S1A' created at: {dbPath}");

        // 4. Import all S1A tables into the global system database
        try
        {
            _context.Log($"[S1A] Importing Segment1A tables from: {xlsxPath}");
            var result = RCS.Cogo.Wpf.Services.JeaImportService.Import(
                xlsxPath, CurrentProject.Id.ToString());

            if (result.Success)
            {
                _context.Log("╔══════════════════════════════════════════════");
                _context.Log("║  Segment 1A Import Summary");
                _context.Log("╠══════════════════════════════════════════════");
                foreach (var s in result.Sheets.Where(sh => sh.Imported > 0 || sh.Warnings.Count > 0))
                {
                    _context.Log($"║  {s.SheetName,-38}: {s.Imported,4} rows  ({s.Skipped} skipped)");
                    foreach (var w in s.Warnings.Take(3))
                        _context.Log($"║     ⚠ {w}");
                }
                _context.Log("╠══════════════════════════════════════════════");
                _context.Log($"║  TOTAL IMPORTED : {result.TotalImported,4}");
                _context.Log($"║  TOTAL SKIPPED  : {result.TotalSkipped,4}");
                _context.Log("╚══════════════════════════════════════════════");

                RefreshData(true);
                _ = LoadInstalledAssetsAsync();

                System.Windows.MessageBox.Show(
                    $"Project '70498-S1A' created and imported successfully!\n\n" +
                    $"Records imported : {result.TotalImported}\n" +
                    $"Records skipped  : {result.TotalSkipped}\n\n" +
                    $"Database saved to:\n{dbPath}\n\n" +
                    $"Use File → Import JEA Template to re-import at any time.",
                    "S1A Import Complete",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            else
            {
                _context.Log($"[S1A] Import failed: {result.ErrorMessage}");
                System.Windows.MessageBox.Show(
                    $"Import failed:\n{result.ErrorMessage}",
                    "Import Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _context.Log($"[S1A] Import exception: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Import failed:\n{ex.Message}",
                "Import Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void ImportPointsList()
    {
        if (!EnsureActiveProject()) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Text Files (*.txt;*.csv)|*.txt;*.csv|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var lines = File.ReadAllLines(dialog.FileName);
                var newSurveyPoints = new List<RCS.Data.Entities.SurveyPoint>();

                int count = 0;
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    // Simple PNEZD parser: 100 5000 5000 100 Desc
                    // Supports comma, tab, or space separation
                    var parts = line.Split(new[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // Need at least 3 parts for NEZ, usually 5 for PNEZD
                    if (parts.Length >= 3)
                    {
                        var id = parts[0];
                        double n, e, z = 0;
                        string desc = "";

                        // Check if first part is ID (string) or Coordinate
                        // Standard: ID N E Z D
                        if (double.TryParse(parts[1], out n) && double.TryParse(parts[2], out e))
                        {
                            if (parts.Length > 3) double.TryParse(parts[3], out z);
                            if (parts.Length > 4) desc = string.Join(" ", parts.Skip(4));
                            
                            _context.AddPoint(id, new Point3D(n, e, z), desc);
                            
                            newSurveyPoints.Add(new RCS.Data.Entities.SurveyPoint 
                            { 
                                Id = $"{CurrentProject.Id}_{id}", 
                                PointNumber = id,
                                ProjectId = CurrentProject.Id.ToString(), 
                                Northing = n, Easting = e, Elevation = z, Description = desc 
                            });
                            
                            count++;
                        }
                    }
                }
                
                // Method 1: Push points to Survey Linework Entity Framework DB for Figures foreign keys
                try 
                {
                    using (var db = new RCS.Data.AppDbContext())
                    {
                        var existing = db.SurveyPoints.Where(p => p.ProjectId == CurrentProject.Id.ToString()).Select(p => p.Id).ToList();
                        var toInsert = newSurveyPoints.Where(p => !existing.Contains(p.Id)).ToList();
                        
                        // We also need to update existing ones if coordinates changed to be totally robust
                        var toUpdateIds = newSurveyPoints.Where(p => existing.Contains(p.Id)).ToList();
                        foreach(var upPt in toUpdateIds)
                        {
                            var tracking = db.SurveyPoints.Local.FirstOrDefault(x => x.Id == upPt.Id) ?? db.SurveyPoints.FirstOrDefault(x => x.Id == upPt.Id);
                            if (tracking != null)
                            {
                                tracking.Northing = upPt.Northing;
                                tracking.Easting = upPt.Easting;
                                tracking.Elevation = upPt.Elevation;
                                tracking.Description = upPt.Description;
                            }
                        }

                        if (toInsert.Any() || toUpdateIds.Any())
                        {
                            db.SurveyPoints.AddRange(toInsert);
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception dbEx)
                {
                    string inner = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                    _context.Log($"[AUDIT] Warning: Could not sync points with Method 1 SQLite DB: {dbEx.Message}. Inner: {inner}");
                }

                _context.Log($"[AUDIT] Imported {count} points from {dialog.FileName}");
                RefreshData();
            }
            catch (Exception ex)
            {
                _context.Log($"[AUDIT] Error importing points: {ex.Message}");
                System.Windows.MessageBox.Show($"Error importing points: {ex.Message}");
            }
        }
    }

}
