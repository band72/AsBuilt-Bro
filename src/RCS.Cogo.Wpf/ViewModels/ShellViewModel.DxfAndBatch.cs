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
    private void ImportDxfLinework()
    {
        if (CurrentProject == null)
        {
            System.Windows.MessageBox.Show("Please open or create a project first.", "No Project", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "DXF Files (*.dxf)|*.dxf|All Files (*.*)|*.*",
            DefaultExt = ".dxf"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(dialog.FileName);
                bool inEntities = false;
                string currentEntity = "";
                string currentLayer = "0";
                
                double curX = 0, curY = 0, curX2 = 0, curY2 = 0;
                var lwPoints = new System.Collections.Generic.List<RCS.Cogo.Core.Primitives.Point3D>();
                var allDxfEntities = new System.Collections.Generic.List<DxfEntity>();
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string code = lines[i].Trim();
                    string val = (i + 1 < lines.Length) ? lines[++i].Trim() : "";

                    if (code == "0" && val == "SECTION")
                    {
                        var nextVal = lines[i + 2].Trim();
                        if (nextVal == "ENTITIES") inEntities = true;
                    }
                    else if (code == "0" && val == "ENDSEC")
                    {
                        inEntities = false;
                    }
                    
                    if (!inEntities) continue;
                    
                    if (code == "0")
                    {
                        if (currentEntity == "LINE")
                        {
                            var pts = new System.Collections.Generic.List<RCS.Cogo.Core.Primitives.Point3D> { 
                                new RCS.Cogo.Core.Primitives.Point3D(curY, curX, 0), 
                                new RCS.Cogo.Core.Primitives.Point3D(curY2, curX2, 0) 
                            };
                            allDxfEntities.Add(new DxfEntity { Type = "LINE", Layer = currentLayer, Points = pts });
                        }
                        else if (currentEntity == "LWPOLYLINE" && lwPoints.Count > 0)
                        {
                            allDxfEntities.Add(new DxfEntity { Type = "LWPOLYLINE", Layer = currentLayer, Points = lwPoints });
                            lwPoints = new System.Collections.Generic.List<RCS.Cogo.Core.Primitives.Point3D>();
                        }
                        
                        currentEntity = val;
                        curX = curY = curX2 = curY2 = 0;
                        currentLayer = "0";
                    }
                    else if (code == "8")
                    {
                        currentLayer = val;
                    }
                    else
                    {
                        if (currentEntity == "LINE")
                        {
                            if (code == "10") double.TryParse(val, out curX);
                            if (code == "20") double.TryParse(val, out curY);
                            if (code == "11") double.TryParse(val, out curX2);
                            if (code == "21") double.TryParse(val, out curY2);
                        }
                        else if (currentEntity == "LWPOLYLINE")
                        {
                            if (code == "10") double.TryParse(val, out curX);
                            if (code == "20") 
                            {
                                double.TryParse(val, out curY);
                                lwPoints.Add(new RCS.Cogo.Core.Primitives.Point3D(curY, curX, 0));
                            }
                        }
                    }
                }
                
                // Finalize last entity
                if (currentEntity == "LINE")
                {
                    allDxfEntities.Add(new DxfEntity { Type = "LINE", Layer = currentLayer, Points = new System.Collections.Generic.List<RCS.Cogo.Core.Primitives.Point3D> { new RCS.Cogo.Core.Primitives.Point3D(curY, curX, 0), new RCS.Cogo.Core.Primitives.Point3D(curY2, curX2, 0) } });
                }
                else if (currentEntity == "LWPOLYLINE" && lwPoints.Count > 0)
                {
                    allDxfEntities.Add(new DxfEntity { Type = "LWPOLYLINE", Layer = currentLayer, Points = lwPoints });
                }

                var uniqueLayers = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Distinct(System.Linq.Enumerable.Select(allDxfEntities, e => e.Layer)));
                if (uniqueLayers.Count == 0)
                {
                    System.Windows.MessageBox.Show("No linework found in DXF.", "DXF Import");
                    return;
                }

                var layerWindow = new RCS.Cogo.Wpf.Views.DxfLayerSelectWindow(uniqueLayers)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                
                if (layerWindow.ShowDialog() != true || layerWindow.SelectedLayers.Count == 0)
                {
                    return; // Canceled
                }
                
                var selectedLayers = layerWindow.SelectedLayers;
                var entitiesToImport = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(allDxfEntities, e => selectedLayers.Contains(e.Layer)));
                
                int newPointsCount = System.Linq.Enumerable.Sum(entitiesToImport, e => e.Points.Count);
                if (newPointsCount > 1000)
                {
                    System.Windows.MessageBox.Show($"The selected layers contain {newPointsCount} vertices, which exceeds the core database safety limit of 1000 per import. Please select fewer layers.", "Import Limit Exceeded", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                    return;
                }

                using (var db = new RCS.Data.AppDbContext())
                {
                    var existingDbPoints = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(db.SurveyPoints, sp => sp.ProjectId == CurrentProject.Id.ToString()));
                    int maxPointNum = 90000;
                    
                    var numberedPts = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Where(existingDbPoints, p => int.TryParse(p.PointNumber, out int _)));
                    if (numberedPts.Count > 0)
                    {
                        int currentHighest = System.Linq.Enumerable.Max(numberedPts, p => int.Parse(p.PointNumber));
                        maxPointNum = System.Math.Max(maxPointNum, currentHighest + 1);
                    }

                    int importedFigsCount = 0;
                    
                    foreach (var entity in entitiesToImport)
                    {
                        var figureVertexList = new System.Collections.Generic.List<RCS.Data.Entities.FigureVertex>();
                        int order = 0;
                        string newGuid = System.Guid.NewGuid().ToString();
                        string figName = $"DXF_{entity.Type}_{newGuid.Substring(0, 4)}";
                        var memFigure = new RCS.Cogo.App.State.Figure(figName);
                        
                        foreach (var pt in entity.Points)
                        {
                            var matchedPoint = System.Linq.Enumerable.FirstOrDefault(existingDbPoints, p => 
                                System.Math.Abs(p.Northing - pt.Northing) <= 0.01 && 
                                System.Math.Abs(p.Easting - pt.Easting) <= 0.01);
                                
                            string targetPointId = "";
                            
                            if (matchedPoint != null)
                            {
                                targetPointId = matchedPoint.Id;
                            }
                            else
                            {
                                var newPt = new RCS.Data.Entities.SurveyPoint
                                {
                                    ProjectId = CurrentProject.Id.ToString(),
                                    PointNumber = maxPointNum.ToString(),
                                    Northing = pt.Northing,
                                    Easting = pt.Easting,
                                    Elevation = 0,
                                    Description = $"DXF_IMP_{entity.Layer}"
                                };
                                db.SurveyPoints.Add(newPt);
                                existingDbPoints.Add(newPt); 
                                targetPointId = newPt.Id;
                                maxPointNum++;
                                
                                _context.AddPoint(targetPointId, new RCS.Cogo.Core.Primitives.Point3D(pt.Northing, pt.Easting, 0), newPt.Description);
                            }

                            figureVertexList.Add(new RCS.Data.Entities.FigureVertex
                            {
                                Id = System.Guid.NewGuid().ToString(),
                                PointId = targetPointId,
                                OrderIndex = order++,
                                Bulge = 0
                            });
                            memFigure.AddPoint(targetPointId);
                        }
                        
                        var newFigure = new RCS.Data.Entities.Figure
                        {
                            Id = newGuid,
                            ProjectId = CurrentProject.Id.ToString(),
                            Name = figName,
                            Layer = entity.Layer,
                            DescriptionText = "DXF Imported Figure",
                            IsClosed = false, 
                            Vertices = figureVertexList,
                            Discipline = "Survey",
                            FeatureType = "DXF"
                        };
                        db.Figures.Add(newFigure);
                        _context.AddFigure(memFigure);
                        importedFigsCount++;
                        
                        // Let RefreshData handle view rendering
                        // Figures.Add(new FigureViewModel(newFigure.Name, entity.Points, System.Windows.Media.Brushes.Cyan));
                    }
                    
                    db.SaveChanges();
                    CommandLog.Add($"Imported {importedFigsCount} DXF entities mapping {newPointsCount} virtual points to Database.");
                    
                    RefreshData(true);
                    ZoomExtentsRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error importing DXF: {ex.Message}");
                System.Windows.MessageBox.Show($"Error importing DXF: {ex.Message}", "DXF Import Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    private void ExportDxf()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "DXF File (*.dxf)|*.dxf|All Files (*.*)|*.*",
            DefaultExt = ".dxf",
            FileName = "Project.dxf"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var writer = new RCS.Cogo.Wpf.Services.ProfessionalDxfWriter();
                writer.Begin();
                
                // Build a fast lookup: firstWord(description) → CogoCode
                var codeMap = CogoCodes
                    .Where(c => !string.IsNullOrWhiteSpace(c.LocalCode))
                    .ToDictionary(c => c.LocalCode.ToUpperInvariant(), c => c,
                                  StringComparer.OrdinalIgnoreCase);

                // Export Points — with block symbol if a Block is assigned to the code
                foreach (var p in _context.GetAllPoints())
                {
                    // Resolve the code from the first word of the description
                    string firstWord = (p.Description ?? "").Split(' ', 2)[0].ToUpperInvariant();
                    codeMap.TryGetValue(firstWord, out var matchedCode);

                    bool hasBlock = matchedCode != null &&
                                   !string.IsNullOrWhiteSpace(matchedCode.Block);

                    if (hasBlock)
                    {
                        // DXF INSERT — the block name is the .dwg filename without extension
                        string blockName = matchedCode!.Block;
                        double scale     = matchedCode.BlockScale > 0 ? matchedCode.BlockScale : 1.0;
                        string layer     = $"CODE_{matchedCode.LocalCode}";
                        writer.InsertBlock(blockName, p.Point.Easting, p.Point.Northing, scale, layer);
                    }
                    else
                    {
                        // Fallback: plain DXF POINT entity
                        writer.AddPoint(p.Point, "POINTS");
                    }

                    // Always write the point ID and description labels
                    writer.AddText(p.Id,          p.Point.Easting + 1, p.Point.Northing + 1, 0.5, "POINT_IDS");
                    writer.AddText(p.Description ?? string.Empty, p.Point.Easting + 1, p.Point.Northing - 1, 0.4, "POINT_DESC");
                }
                
                // Export Figures (Pipes)
                foreach(var fig in Figures)
                {
                    if (fig.Points.Count >= 2)
                    {
                        for (int i=0; i < fig.Points.Count - 1; i++)
                        {
                            var p1 = fig.Points[i];
                            var p2 = fig.Points[i+1];
                            writer.AddLine(p1.X, p1.Y, p2.X, p2.Y, "FIGURES");
                        }
                    }
                    
                    if (ShowFigureLabels)
                    {
                        foreach(var label in fig.Labels)
                        {
                            writer.AddText(label.Text, label.Easting, label.Northing, 1.0, "FIGURE_LABELS", "CENTER", label.RotationDegrees);
                        }
                    }
                }
                
                // Export PipeRuns
                foreach (var run in PipeRuns)
                {
                    var p1 = _context.GetPoint(run.FromPointId);
                    var p2 = _context.GetPoint(run.ToPointId);
                    if (p1 != null && p2 != null)
                    {
                        string layer = $"PIPE_{run.Type}_{run.Diameter}_{run.Material}";
                        writer.AddLine(p1.Easting, p1.Northing, p2.Easting, p2.Northing, layer);
                        
                        // Add textual label at mid point
                        double midX = (p1.Easting + p2.Easting) / 2;
                        double midY = (p1.Northing + p2.Northing) / 2;
                        writer.AddText($"{run.Diameter}\" {run.Material} {run.Type}", midX, midY, 1.5, layer + "_TEXT", "CENTER");
                    }
                }
                
                // ── Discipline color map (ACI codes) ────────────────────────────────────
                // 1=Red 2=Yellow 3=Green 4=Cyan 5=Blue 6=Magenta 30=Orange 256=ByLayer
                static int DisciplineColor(string typeTag)
                {
                    string t = typeTag.ToUpper();
                    if (t.StartsWith("WW") || t.Contains("SEW") || t.Contains("SAN")) return 3;  // Green
                    if (t.StartsWith("W ") || t.StartsWith("W_") || t == "W" || t.Contains("WATER")) return 5; // Blue
                    if (t.StartsWith("R ") || t.StartsWith("R_") || t == "R" || t.Contains("RECLAIM")) return 6; // Magenta
                    if (t.StartsWith("ST") || t.Contains("STORM") || t.Contains("DRAIN")) return 4; // Cyan
                    if (t.StartsWith("E") || t.Contains("ELEC") || t.Contains("POLE")) return 1;  // Red
                    if (t.StartsWith("G") || t.Contains("GAS")) return 30; // Orange
                    if (t.Contains("CHIL")) return 141; // Light Blue
                    return 256; // ByLayer (default)
                }

                // Export Structures — block + colored label per discipline
                foreach (var s in StructureGraphics)
                {
                    string block = "MANHOLE";
                    string t = (s.Type ?? "").ToUpper();
                    int dColor = DisciplineColor(s.Type ?? "");

                    if (t.Contains("VALVE") || t.EndsWith("V") || t == "WV" || t == "WWV") block = "VALVE";
                    else if (t.Contains("HYDRANT") || t.EndsWith("H") || t == "HYD")        block = "HYDRANT";
                    else if (t.Contains("METER") || t.Contains("MET"))                       block = "METER";
                    else if (t.Contains("POLE"))                                              block = "POLE";
                    else if (t.Contains("BOX") || t.Contains("VAULT"))                       block = "BOX";
                    else if (t.Contains("FITTING") || t.EndsWith("F"))                       block = "FITTING";

                    string structLayer = $"STRUCT_{(s.Type ?? "DEFAULT").ToUpper().Replace(" ", "_")}";
                    writer.InsertBlock(block, s.Easting, s.Northing, 1.0, structLayer, dColor);
                    writer.AddText(s.Label, s.Easting + 1.5, s.Northing + 3.0, 1.2,
                                   structLayer + "_LBL", "LEFT", 0, dColor);
                }

                // Export water & reclaimed pipe backbone (start→end GPS)
                if (InstalledAssets != null)
                {
                    void ExportPipes<T>(System.Collections.ObjectModel.ObservableCollection<T> pipes, string layerPrefix, int col)
                        where T : RCS.Data.Entities.InstalledAsset
                    {
                        foreach (var pipe in pipes)
                        {
                            double sn = pipe.StartNorthing ?? 0, se = pipe.StartEasting ?? 0;
                            double en = pipe.EndNorthing   ?? 0, ee = pipe.EndEasting   ?? 0;
                            if (Math.Abs(sn) < 0.001 && Math.Abs(se) < 0.001) continue;
                            if (Math.Abs(en) < 0.001 && Math.Abs(ee) < 0.001) continue;
                            string pipeLayer = $"{layerPrefix}_{pipe.Size ?? ""}_PIPE";
                            writer.AddLine(se, sn, ee, en, pipeLayer, col);
                            // Midpoint label
                            writer.AddText(
                                $"{pipe.Size} {pipe.Material} {pipe.PartKey}",
                                (se + ee) / 2, (sn + en) / 2, 1.0, pipeLayer + "_LBL", "LEFT", 0, col);
                        }
                    }
                    ExportPipes(InstalledAssets.WaterPipes,     "W",  5);
                    ExportPipes(InstalledAssets.ReclaimedPipes, "R",  6);
                    ExportPipes(InstalledAssets.WWPressurePipes,"WW", 3);

                    // Sewer gravity (manhole-to-manhole)
                    var mhIdx = InstalledAssets.Manholes
                        .Where(m => m.PartKey != null && m.Northing.HasValue && m.Easting.HasValue)
                        .ToDictionary(m => m.PartKey!, m => m);
                    foreach (var pipe in InstalledAssets.WWGravityPipes)
                    {
                        if (!mhIdx.TryGetValue(pipe.UpstreamPointId ?? "", out var up)) continue;
                        if (!mhIdx.TryGetValue(pipe.DownstreamPointId ?? "", out var dn)) continue;
                        writer.AddLine(up.Easting!.Value, up.Northing!.Value,
                                       dn.Easting!.Value, dn.Northing!.Value, "WW_GRAVITY_PIPE", 3);
                    }
                }

                writer.End();
                writer.Save(dialog.FileName);
                _context.Log($"[AUDIT] Exported DXF to {dialog.FileName}");
                System.Windows.MessageBox.Show("DXF Export Successful.");
            }
            catch (Exception ex)
            {
                 _context.Log($"[AUDIT] Error exporting DXF: {ex.Message}");
                 System.Windows.MessageBox.Show($"Error exporting DXF: {ex.Message}");
            }
        }
    }

    // ── Item 4: JEA As-Built COGO Script Export ───────────────────────────────
    private void ExportJeaCogoScript()
    {
        if (InstalledAssets == null || StructureGraphics.Count == 0)
        {
            System.Windows.MessageBox.Show("No JEA assets on canvas to export. Please import first.");
            return;
        }
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "COGO Script (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"JEA_AsBuilt_COGO_{DateTime.Now:yyyyMMdd}"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"// JEA As-Built COGO Script  —  Exported {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"// Project: {CurrentProject?.ProjectName ?? "(none)"}");
            sb.AppendLine($"// CRS: State Plane Florida East (FIPS 0901) / NAD83, US Survey Feet");
            sb.AppendLine($"// Format: n[Northing] e[Easting] [TYPE] [ID]");
            sb.AppendLine();

            // Group by discipline
            var groups = StructureGraphics
                .GroupBy(s => s.SymbolType)
                .OrderBy(g => g.Key);

            foreach (var grp in groups)
            {
                sb.AppendLine($"// ── {grp.Key.ToUpper()} ─────────────────────────────────────");
                foreach (var s in grp.OrderBy(x => x.Label))
                {
                    // Resolve full feature type for COGO tag
                    string cogoType = s.Type.ToUpper().Replace(" ", "_");
                    sb.AppendLine($"n{s.Northing:F4} e{s.Easting:F4} {cogoType} {s.Label}");
                }
                sb.AppendLine();
            }

            // Pipe runs (sewer gravity)
            if (InstalledAssets.WWGravityPipes.Any())
            {
                sb.AppendLine("// ── SEWER GRAVITY PIPES ─────────────────────────────────────");
                var mhIdx = InstalledAssets.Manholes
                    .Where(m => m.PartKey != null && m.Northing.HasValue && m.Easting.HasValue)
                    .ToDictionary(m => m.PartKey!, m => m);
                foreach (var pipe in InstalledAssets.WWGravityPipes)
                {
                    if (!mhIdx.TryGetValue(pipe.UpstreamPointId   ?? "", out var up)) continue;
                    if (!mhIdx.TryGetValue(pipe.DownstreamPointId  ?? "", out var dn)) continue;
                    sb.AppendLine($"// Pipe {pipe.PartKey}  {pipe.Size}\"  {pipe.Material}  Slope:{pipe.Slope:F4}%");
                    sb.AppendLine($"n{up.Northing:F4} e{up.Easting:F4} WW_PIPE_START {pipe.PartKey}");
                    sb.AppendLine($"n{dn.Northing:F4} e{dn.Easting:F4} WW_PIPE_END   {pipe.PartKey}");
                }
                sb.AppendLine();
            }

            // Water pipes with GPS endpoints
            var waterPipesWithCoords = InstalledAssets.WaterPipes
                .Where(p => p.StartNorthing.HasValue && p.StartEasting.HasValue).ToList();
            if (waterPipesWithCoords.Any())
            {
                sb.AppendLine("// ── WATER DISTRIBUTION PIPES ───────────────────────────────");
                foreach (var pipe in waterPipesWithCoords)
                {
                    sb.AppendLine($"// Pipe {pipe.PartKey}  {pipe.Size}\"  {pipe.Material}");
                    sb.AppendLine($"n{pipe.StartNorthing:F4} e{pipe.StartEasting:F4} W_PIPE_START {pipe.PartKey}");
                    sb.AppendLine($"n{pipe.EndNorthing:F4}   e{pipe.EndEasting:F4}   W_PIPE_END   {pipe.PartKey}");
                }
                sb.AppendLine();
            }

            File.WriteAllText(dialog.FileName, sb.ToString());
            _context.Log($"[AUDIT] Exported JEA COGO Script to {dialog.FileName}");
            System.Windows.MessageBox.Show($"COGO Script exported.\n{dialog.FileName}");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Export error: {ex.Message}");
        }
    }

    private void ExportPointsXml()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "LandXML File (*.xml)|*.xml|All Files (*.*)|*.*",
            DefaultExt = ".xml",
            FileName = "Points.xml"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("<?xml version=\"1.0\"?>");
                sb.AppendLine("<LandXML xmlns=\"http://www.landxml.org/schema/LandXML-1.2\" version=\"1.2\" date=\"" + DateTime.Now.ToString("yyyy-MM-dd") + "\" time=\"" + DateTime.Now.ToString("HH:mm:ss") + "\">");
                sb.AppendLine("  <Units>");
                sb.AppendLine("    <Metric areaUnit=\"squareMeter\" linearUnit=\"meter\" volumeUnit=\"cubicMeter\" temperatureUnit=\"celsius\" pressureUnit=\"mmHG\"/>");
                sb.AppendLine("  </Units>");
                sb.AppendLine("  <CgPoints>");
                
                foreach (var p in _context.GetAllPoints())
                {
                    // LandXML CgPoint format: North East Elev (typically) or Y X Z
                    // The standard is Y X Z (North East Elev)
                    sb.AppendLine($"    <CgPoint name=\"{p.Id}\" desc=\"{p.Description}\">{p.Point.Northing:F4} {p.Point.Easting:F4} {p.Point.Elevation:F4}</CgPoint>");
                }
                
                sb.AppendLine("  </CgPoints>");
                sb.AppendLine("</LandXML>");
                
                System.IO.File.WriteAllText(dialog.FileName, sb.ToString());
                CommandLog.Add($"Points exported (LandXML) to: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error exporting points: {ex.Message}");
            }
        }
    }

    private void ImportBatchScript()
    {
        if (!EnsureActiveProject()) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "COGO Script (*.cogo)|*.cogo|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
            DefaultExt = ".cogo"
        };
        
        if (dialog.ShowDialog() == true)
        {
            try 
            {
                BatchScriptContent = System.IO.File.ReadAllText(dialog.FileName);
                CommandLog.Add($"Loaded script: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                CommandLog.Add($"Error reading file: {ex.Message}");
            }
        }
    }

    private async Task RunBatchScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(BatchScriptContent)) return;

        if (BatchScriptContent.Contains("PIPE-ENGINE-ON", StringComparison.OrdinalIgnoreCase) || 
            BatchScriptContent.Contains("PRUN", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show("This script contains Piping Commands. Please run it through the Piping Script tab instead.", "Piping Script Detected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsRunningScript = true;
        
        // As requested: display progress bar for 1.5 seconds minimum
        await Task.Delay(1500);

        CommandLog.Add("--- Running Batch Script ---");
        var lines = BatchScriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        // Check for RESET directives at the top of the script
        bool shouldReset = true;
        foreach (var line in lines)
        {
            string t = line.Trim().ToUpper();
            if (string.IsNullOrEmpty(t) || t.StartsWith("!") || t.StartsWith("//")) continue;
            
            if (t == "RESET-OFF") 
            {
                shouldReset = false;
                break;
            }
            if (t == "RESET-ON")
            {
                shouldReset = true;
                break;
            }
            // If the first real command is something else, stop looking and default to true
            break;
        }

        if (shouldReset)
        {
            // Reset state before running
            await _engine.ExecuteAsync("CLEAR", _context);
            await _engine.ExecuteAsync("DEL PTS", _context);
            await _engine.ExecuteAsync("DEL FIG", _context);
        }
        int counter = 0;
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/"))
                continue;
                
            CommandLog.Add($"> {trimmed}");
            await _engine.ExecuteAsync(trimmed, _context);
            
            counter++;
            if (counter % 50 == 0) await Task.Delay(1); // Force WPF dispatcher to render progress bar and unblock UI
        }
        
        CommandLog.Add("--- Batch Complete ---");
        RefreshData();
        
        IsRunningScript = false;
    }

    private async Task WalkBatchScriptAsync()
    {
        if (string.IsNullOrWhiteSpace(BatchScriptContent)) return;

        if (BatchScriptContent.Contains("PIPE-ENGINE-ON", StringComparison.OrdinalIgnoreCase) || 
            BatchScriptContent.Contains("PRUN", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show("This script contains Piping Commands. Please run it through the Piping Script tab instead.", "Piping Script Detected", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        CommandLog.Add("--- Walking Batch Script ---");
        
        var lines = BatchScriptContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        bool shouldReset = true;
        foreach (var line in lines)
        {
            string t = line.Trim().ToUpper();
            if (string.IsNullOrEmpty(t) || t.StartsWith("!") || t.StartsWith("//")) continue;
            
            if (t == "RESET-OFF") 
            {
                shouldReset = false;
                break;
            }
            if (t == "RESET-ON")
            {
                shouldReset = true;
                break;
            }
            break;
        }

        if (shouldReset)
        {
            // Reset state before walking
            await _engine.ExecuteAsync("CLEAR", _context);
            await _engine.ExecuteAsync("DEL PTS", _context);
            await _engine.ExecuteAsync("DEL FIG", _context);
            RefreshData(true);
        }
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("/"))
                continue;
                
            CommandLog.Add($"> {trimmed}");
            await _engine.ExecuteAsync(trimmed, _context);
            
            // Force data refresh but don't reset bounds
            RefreshData(false);

            System.Windows.Point zoomTarget = new System.Windows.Point();
            bool foundTarget = false;
            
            if (_context.CurrentFigure != null && _context.CurrentFigure.PointIds.Count > 0)
            {
                var pt = _context.GetPoint(_context.CurrentFigure.PointIds.LastOrDefault() ?? "");
                if (pt != null)
                {
                    zoomTarget = new System.Windows.Point(pt.Easting, pt.Northing);
                    foundTarget = true;
                }
            }

            if (!foundTarget)
            {
                var lastPt = Points.LastOrDefault();
                if (lastPt != null)
                {
                    zoomTarget = new System.Windows.Point(lastPt.Easting, lastPt.Northing);
                    foundTarget = true;
                }
            }

            if (foundTarget)
            {
                 System.Windows.Application.Current?.Dispatcher.Invoke(() => 
                    ZoomToPointRequested?.Invoke(this, zoomTarget));
            }
            
            await Task.Delay(1000); // 1-second delay for visually tracking the build
        }
        
        CommandLog.Add("--- Walk Complete ---");
        RefreshData(true);
    }

    private async Task ExecuteCommandAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandInput)) return;

        string cmd = CommandInput;
        CommandInput = ""; // Clear input immediately
        
        CommandLog.Add($"> {cmd}"); // Echo command to Command Log

        await _engine.ExecuteAsync(cmd, _context);

        RefreshData();
    }

    public void RefreshData(bool autoZoomExtents = true)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() => 
        {
            // Update Points
            Points.Clear();
            var validCodes = CogoCodes.Select(c => c.LocalCode).ToList();
            if (validCodes.Count == 0) 
            {
                // If no codes loaded, maybe everything is valid? Or nothing?
                // User requirement: "Highlight in red missing or incorrect matches."
                // So if no code database, everything is red.
            }

            foreach (var p in _context.GetAllPoints())
            {
                Points.Add(new PointViewModel(p.Id, p.Point, p.Description, validCodes));
            }

            // Update Figures
            Figures.Clear();
            foreach (var fig in _context.GetAllFigures())
            {
                var existingAsset = InstalledAssets?.FigureAssets?.FirstOrDefault(f => string.Equals(f.Name, fig.Name, StringComparison.OrdinalIgnoreCase));
                if (existingAsset != null && !existingAsset.IsVisible) continue; // Skip rendering hidden figures

                bool isClosed = fig.PointIds.Count > 2 && fig.PointIds.First() == fig.PointIds.Last();
                var pts = new System.Collections.Generic.List<Point3D>();
                foreach (var id in fig.PointIds)
                {
                    var p = _context.GetPoint(id);
                    if (p != null) pts.Add(p);
                }
                
                if (isClosed)
                {
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
                        // continue; // Skip rendering
                    }
                }
                
                if (pts.Count > 1)
                {
                    // ── Adaptive crosslink detection (5× median segment length) ──
                    var figIds = fig.PointIds;
                    var rDists = new System.Collections.Generic.List<double>();
                    for (int i = 0; i < pts.Count - 1; i++)
                    {
                        double dx = pts[i + 1].Easting - pts[i].Easting;
                        double dy = pts[i + 1].Northing - pts[i].Northing;
                        rDists.Add(Math.Sqrt(dx * dx + dy * dy));
                    }
                    var rSorted = new System.Collections.Generic.List<double>(rDists); rSorted.Sort();
                    double rMedian = rSorted[rSorted.Count / 2];
                    double rCutoff = Math.Max(rMedian * 2.0, 110.0);

                    bool hasCrosslink = false;
                    for (int i = 0; i < rDists.Count; i++)
                    {
                        if (rDists[i] > rCutoff)
                        {
                            hasCrosslink = true;
                            string fromId = i < figIds.Count ? figIds[i] : "?";
                            string toId   = (i + 1) < figIds.Count ? figIds[i + 1] : "?";
                            _context.Log($"[⚠ CROSSLINK] Figure '{fig.Name}': pt {fromId}→{toId}, dist={rDists[i]:F0}ft (cutoff {rCutoff:F0}ft)");
                        }
                    }
                    fig.IsInvalidCrosslink = hasCrosslink;

                    System.Windows.Media.Brush stroke;
                    if (fig.MapCheckFailed)
                        stroke = System.Windows.Media.Brushes.Red;
                    else if (fig.IsInvalidCrosslink)
                        stroke = new System.Windows.Media.SolidColorBrush(
                            System.Windows.Media.Color.FromRgb(0xFF, 0x88, 0x00)); // Orange
                    else
                        stroke = System.Windows.Media.Brushes.Yellow;

                    Figures.Add(new FigureViewModel(fig.Name, pts, stroke, fig.Labels));
                }
            }
            
            // Render Pipes with Colors
            foreach(var run in PipeRuns)
            {
                var pStart = _context.GetPoint(run.FromPointId);
                var pEnd = _context.GetPoint(run.ToPointId);
                if (pStart != null && pEnd != null)
                {
                    var pts = new System.Collections.Generic.List<Point3D> { pStart, pEnd };
                    
                    // Color Logic based on Service/Type
                    System.Windows.Media.Brush pipeBrush = System.Windows.Media.Brushes.Gray;
                    
                    var type = run.Type?.ToUpper() ?? "";
                    if (type == "WW" || type.Contains("SAN")) pipeBrush = System.Windows.Media.Brushes.Green;
                    else if (type == "ST" || type == "S" || type == "D" || type.Contains("SW") || type.Contains("STORM")) pipeBrush = System.Windows.Media.Brushes.Cyan;
                    else if (type == "W" || type.Contains("WATER")) pipeBrush = System.Windows.Media.Brushes.Blue;
                    else if (type == "R" || type.Contains("RECLAIM")) pipeBrush = System.Windows.Media.Brushes.Purple;
                    else if (type == "G" || type.Contains("GAS")) pipeBrush = System.Windows.Media.Brushes.Orange;
                    else if (type == "E" || type == "EL" || type.Contains("ELEC")) pipeBrush = System.Windows.Media.Brushes.Red;
                    else if (type == "CH" || type.Contains("CHILL")) pipeBrush = System.Windows.Media.Brushes.LightSkyBlue;
                    else if (type.Contains("PP") || type.Contains("PRESS")) pipeBrush = System.Windows.Media.Brushes.Red;
                    
                    string code = "UNK";
                    if (type == "WW" || type.Contains("SAN")) code = "WW";
                    else if (type == "W" || type.Contains("WATER")) code = "WA";
                    else if (type == "R" || type.Contains("RECLAIM")) code = "RC";
                    else if (type == "G" || type.Contains("GAS")) code = "GS";
                    else if (type == "E" || type == "EL" || type.Contains("ELEC")) code = "EL";
                    else if (type == "CH" || type.Contains("CHILL")) code = "CH";
                    else if (type == "ST" || type == "S" || type == "D" || type.Contains("SW") || type.Contains("STORM")) code = "DR";

                    var fig = new FigureViewModel($"Pipe-[{code}]-{run.Id}", pts, pipeBrush);
                    Figures.Add(fig);
                }
            }
            
            // Render Alignments
            foreach (var algn in _context.GetAllAlignments())
            {
                var pts = new System.Collections.Generic.List<Point3D>();
                var labels = new System.Collections.Generic.List<RCS.Cogo.App.State.FigureLabel>();

                string FormatStation(double st)
                {
                    int hundreds = (int)(st / 100);
                    double remainder = st - (hundreds * 100);
                    return $"{hundreds}+{remainder:00.00}";
                }

                if (_context.ShowAlignmentLabels && algn.Elements.Count > 0)
                {
                    // Start Label
                    var startPt = algn.Elements[0].GetCoordinateAt(algn.StartStation);
                    labels.Add(new RCS.Cogo.App.State.FigureLabel { Text = $"POB {FormatStation(algn.StartStation)}", Easting = startPt.Easting, Northing = startPt.Northing, RotationDegrees = 0 });
                }

                foreach (var element in algn.Elements)
                {
                    if (element is RCS.Alignments.Core.ArcElement)
                    {
                        // Interpolate arcs for smooth drawing
                        double step = 10.0;
                        for (double s = element.StartStation; s < element.EndStation; s += step)
                        {
                            pts.Add(element.GetCoordinateAt(s));
                        }
                        pts.Add(element.GetCoordinateAt(element.EndStation)); // Ensure flush fit
                    }
                    else
                    {
                        // Lines just need ends
                        pts.Add(element.GetCoordinateAt(element.StartStation));
                        pts.Add(element.GetCoordinateAt(element.EndStation));
                    }

                    if (_context.ShowAlignmentLabels)
                    {
                        // Midpoint
                        double midStation = element.StartStation + (element.Length / 2);
                        var midPt = element.GetCoordinateAt(midStation);
                        labels.Add(new RCS.Cogo.App.State.FigureLabel { Text = $"MID {FormatStation(midStation)}", Easting = midPt.Easting, Northing = midPt.Northing, RotationDegrees = 0 });

                        // Endpoint
                        var endPt = element.GetCoordinateAt(element.EndStation);
                        labels.Add(new RCS.Cogo.App.State.FigureLabel { Text = $"PT {FormatStation(element.EndStation)}", Easting = endPt.Easting, Northing = endPt.Northing, RotationDegrees = 0 });
                    }
                }
                
                if (pts.Count > 1)
                {
                    Figures.Add(new FigureViewModel($"ALGN-{algn.Name}", pts, System.Windows.Media.Brushes.Cyan, labels));
                }
            }

            // Structures
            StructureGraphics.Clear();
            var renderedPoints = new HashSet<string>();
            var allPts = _context.GetAllPoints().ToDictionary(p => p.Id, p => p, StringComparer.OrdinalIgnoreCase);

            foreach(var s in Structures) // Note: s is PipeStructure model
            {
                if (allPts.TryGetValue(s.PointId, out var pData))
                {
                    string combinedType = $"{s.Type} {pData.Description}".Trim();
                    StructureGraphics.Add(new StructureViewModel(s.PointId, pData.Point, combinedType));
                    renderedPoints.Add(s.PointId);
                }
            }

            // Map standard Cogo points into symbol graphics if their descriptions match a predefined code
            foreach(var p in _context.GetAllPoints())
            {
                if (!renderedPoints.Contains(p.Id) && !string.IsNullOrWhiteSpace(p.Description))
                {
                    var sym = new StructureViewModel(p.Id, p.Point, p.Description);
                    if (sym.SymbolType != "Default" || validCodes.Contains(p.Description, StringComparer.OrdinalIgnoreCase))
                    {
                        StructureGraphics.Add(sym);
                        renderedPoints.Add(p.Id);
                    }
                }
            }

            // Auto-Zoom Extents after refresh
            if (autoZoomExtents) ZoomExtentsRequested?.Invoke(this, EventArgs.Empty);

            // These assets come from the JEA Excel import and live in the SQLite DB.
            // They are not in the CogoContext, so we render them directly here.
            if (InstalledAssets != null && _currentProject != null)
            {
                void RenderAssets<T>(System.Collections.ObjectModel.ObservableCollection<T> collection, string typeTag)
                    where T : RCS.Data.Entities.InstalledAsset
                {
                    foreach (var asset in collection)
                    {
                        double n = asset.Northing ?? 0;
                        double e = asset.Easting ?? 0;
                        if (Math.Abs(n) < 0.001 && Math.Abs(e) < 0.001) continue; // skip 0,0
                        var pt = new Point3D(n, e, asset.GradeElevation ?? 0);

                        // Build the canvas label (short ID only)
                        string canvasLabel = asset.PartKey ?? asset.Id.ToString();

                        // Build full attribute dictionary for the click-to-inspect popup
                        // Build full attribute collection for the click-to-inspect popup
                        var data = new System.Collections.ObjectModel.ObservableCollection<InspectorField>
                        {
                            new InspectorField("ID / Part Key",  asset.PartKey ?? "(none)", isReadOnly: true),
                            new InspectorField("Feature Type",   asset.FeatureType ?? typeTag, isReadOnly: true),
                            new InspectorField("Discipline",     asset.Discipline ?? "-", isReadOnly: false),
                            new InspectorField("Subtype",        asset.Subtype ?? "-", isReadOnly: false),
                            new InspectorField("Facility Owner", asset.FacilityOwner ?? "-", isReadOnly: false),
                            new InspectorField("Size",           asset.Size ?? "-", isReadOnly: false),
                            new InspectorField("Material",       asset.Material ?? "-", isReadOnly: false),
                            new InspectorField("Northing (Y)",   n.ToString("F2"), isReadOnly: true),
                            new InspectorField("Easting (X)",    e.ToString("F2"), isReadOnly: true),
                            new InspectorField("Latitude",       asset.Latitude.HasValue ? asset.Latitude.Value.ToString("F6") : "-", isReadOnly: true),
                            new InspectorField("Longitude",      asset.Longitude.HasValue ? asset.Longitude.Value.ToString("F6") : "-", isReadOnly: true),
                            new InspectorField("Grade Elev.",    asset.GradeElevation.HasValue ? asset.GradeElevation.Value.ToString("F2") : "-", isReadOnly: false),
                            new InspectorField("Depth",          asset.Depth.HasValue ? asset.Depth.Value.ToString("F2") : "-", isReadOnly: false)
                        };

                        if (asset.Manufacturer != null) data.Add(new InspectorField("Manufacturer", asset.Manufacturer, isReadOnly: false));
                        if (asset.ValveType != null)    data.Add(new InspectorField("Valve Type", asset.ValveType, isReadOnly: false));
                        if (asset.OpenDirection != null) data.Add(new InspectorField("Open Direction", asset.OpenDirection, isReadOnly: false));
                        if (asset.TurnsToOpen.HasValue) data.Add(new InspectorField("Turns To Open", asset.TurnsToOpen.Value.ToString("F1"), isReadOnly: false));
                        if (asset.ManholeType != null)  data.Add(new InspectorField("Manhole Type", asset.ManholeType, isReadOnly: false));
                        if (asset.RimElevation.HasValue) data.Add(new InspectorField("Rim Elev.", asset.RimElevation.Value.ToString("F2"), isReadOnly: false));
                        if (asset.LowestInvertElevation.HasValue) data.Add(new InspectorField("Lowest Invert", asset.LowestInvertElevation.Value.ToString("F2"), isReadOnly: false));
                        if (asset.LiningMaterial != null) data.Add(new InspectorField("Lining Material", asset.LiningMaterial, isReadOnly: false));
                        if (asset.RfidBarcode != null) data.Add(new InspectorField("RFID / Barcode", asset.RfidBarcode, isReadOnly: false));

                        var typeLabel = $"{typeTag} {canvasLabel}".Trim();
                        var sym = new StructureViewModel(canvasLabel, pt, typeLabel, canvasLabel, data, asset);
                        // Wire click-to-inspect
                        sym.SelectCommand = new RelayCommand(_ => SelectedStructure = sym);
                        StructureGraphics.Add(sym);
                    }
                }



                // Water
                RenderAssets(InstalledAssets.WaterHydrants,   "W HYDRANT");
                RenderAssets(InstalledAssets.WaterValves,     "W VALVE");
                RenderAssets(InstalledAssets.WaterFittings,   "WF");
                RenderAssets(InstalledAssets.WaterMeters,     "WMET");
                RenderAssets(InstalledAssets.WaterLocateBoxes,"W BOX");
                RenderAssets(InstalledAssets.WaterPoints,     "W");

                // Wastewater
                RenderAssets(InstalledAssets.Manholes,        "WW MANHOLE");
                RenderAssets(InstalledAssets.WWFittings,      "WWF");
                RenderAssets(InstalledAssets.WWValves,        "WWV");
                RenderAssets(InstalledAssets.WWPoints,        "WW");
                RenderAssets(InstalledAssets.WWServicePoints, "WW");

                // Reclaimed
                RenderAssets(InstalledAssets.ReclaimedHydrants,"R HYDRANT");
                RenderAssets(InstalledAssets.ReclaimedFittings,"R FITTING");
                RenderAssets(InstalledAssets.ReclaimedValves,  "R VALVE");
                RenderAssets(InstalledAssets.ReclaimedPoints,  "R");

                // Crossings
                foreach (var c in InstalledAssets.PipeCrossings)
                {
                    double n = c.Northing ?? 0; double e = c.Easting ?? 0;
                    if (Math.Abs(n) < 0.001 && Math.Abs(e) < 0.001) continue;
                    var pt = new Point3D(n, e, 0);
                    StructureGraphics.Add(new StructureViewModel(
                        c.CrossingNumber ?? c.Id.ToString(), pt, "CROSSING"));
                }

                // ── Sewer pipe linework: connect manholes via UpstreamPointId / DownstreamPointId ──
                {
                    // Build a lookup: manhole PartKey -> location
                    var mhIndex = InstalledAssets.Manholes
                        .Where(m => m.PartKey != null
                                 && m.Northing.HasValue && m.Easting.HasValue
                                 && (Math.Abs(m.Northing.Value) > 0.001 || Math.Abs(m.Easting.Value) > 0.001))
                        .ToDictionary(m => m.PartKey!, m => m);

                    foreach (var pipe in InstalledAssets.WWGravityPipes)
                    {
                        if (!mhIndex.TryGetValue(pipe.UpstreamPointId ?? "", out var up)) continue;
                        if (!mhIndex.TryGetValue(pipe.DownstreamPointId ?? "", out var dn)) continue;
                        var pts = new System.Collections.Generic.List<Point3D>
                        {
                            new Point3D(up.Northing!.Value, up.Easting!.Value, up.RimElevation ?? 0),
                            new Point3D(dn.Northing!.Value, dn.Easting!.Value, dn.RimElevation ?? 0)
                        };
                        Figures.Add(new FigureViewModel(
                            $"SewerPipe-{pipe.PartKey ?? pipe.Id}",
                            pts,
                            System.Windows.Media.Brushes.Lime));
                    }

                    foreach (var pipe in InstalledAssets.WWPressurePipes)
                    {
                        if (!mhIndex.TryGetValue(pipe.UpstreamPointId ?? "", out var up)) continue;
                        if (!mhIndex.TryGetValue(pipe.DownstreamPointId ?? "", out var dn)) continue;
                        var pts = new System.Collections.Generic.List<Point3D>
                        {
                            new Point3D(up.Northing!.Value, up.Easting!.Value, 0),
                            new Point3D(dn.Northing!.Value, dn.Easting!.Value, 0)
                        };
                        Figures.Add(new FigureViewModel(
                            $"WWPressure-{pipe.PartKey ?? pipe.Id}",
                            pts,
                            System.Windows.Media.Brushes.LimeGreen));
                    }
                }

                // ── Water pipe linework: connect sequential WaterPoints along each pipe run ──
                // WaterPoints have UpstreamPointId linking them to a pipe PartKey.
                // Sort by PartKey prefix and connect in order of GPS position.
                {
                    // Connect water points along same pipe (grouped by UpstreamPointId = PipePartKey)
                    var waterPointsByPipe = InstalledAssets.WaterPoints
                        .Where(p => p.UpstreamPointId != null
                                 && p.Northing.HasValue && p.Easting.HasValue
                                 && (Math.Abs(p.Northing.Value) > 0.001 || Math.Abs(p.Easting.Value) > 0.001))
                        .GroupBy(p => p.UpstreamPointId!);

                    foreach (var grp in waterPointsByPipe)
                    {
                        var ordered = grp.OrderBy(p => p.PartKey).ToList();
                        if (ordered.Count < 2) continue;
                        var pts = ordered.Select(p =>
                            new Point3D(p.Northing!.Value, p.Easting!.Value, p.GradeElevation ?? 0)).ToList();
                        Figures.Add(new FigureViewModel(
                            $"WaterRun-{grp.Key}",
                            pts,
                            System.Windows.Media.Brushes.DeepSkyBlue));
                    }

                    // Also connect WaterFittings + WaterValves to adjacent WaterHydrants
                    // by drawing a tick line from each point asset to its nearest hydrant (within 200ft)
                    var hydrantPts = InstalledAssets.WaterHydrants
                        .Where(h => h.Northing.HasValue && h.Easting.HasValue
                                 && (Math.Abs(h.Northing!.Value) > 0.001 || Math.Abs(h.Easting!.Value) > 0.001))
                        .ToList();

                    var waterAssets = new System.Collections.Generic.List<RCS.Data.Entities.InstalledAsset>();
                    waterAssets.AddRange(InstalledAssets.WaterFittings.Cast<RCS.Data.Entities.InstalledAsset>());
                    waterAssets.AddRange(InstalledAssets.WaterValves.Cast<RCS.Data.Entities.InstalledAsset>());
                    waterAssets.AddRange(InstalledAssets.WaterMeters.Cast<RCS.Data.Entities.InstalledAsset>());

                    foreach (var asset in waterAssets)
                    {
                        double an = asset.Northing ?? 0, ae = asset.Easting ?? 0;
                        if (Math.Abs(an) < 0.001 && Math.Abs(ae) < 0.001) continue;

                        // Find nearest hydrant within 500 survey feet
                        var nearestHyd = hydrantPts
                            .Select(h => new { h, dist = Math.Sqrt(Math.Pow((h.Northing!.Value-an),2)+Math.Pow((h.Easting!.Value-ae),2)) })
                            .Where(x => x.dist < 500)
                            .OrderBy(x => x.dist)
                            .FirstOrDefault();

                        if (nearestHyd != null)
                        {
                            var pts = new System.Collections.Generic.List<Point3D>
                            {
                                new Point3D(an, ae, 0),
                                new Point3D(nearestHyd.h.Northing!.Value, nearestHyd.h.Easting!.Value, 0)
                            };
                            Figures.Add(new FigureViewModel(
                                $"WaterSvc-{asset.PartKey ?? asset.Id}",
                                pts,
                                new System.Windows.Media.SolidColorBrush(
                                    System.Windows.Media.Color.FromRgb(0,160,255))));
                        }
                    }
                }
            }


        });
    }

    // ── Project Lifecycle ────────────────────────────────────────────────────

    /// <summary>Returns true if we can safely discard the current project
    /// (either it's clean, or the user chose to save/discard).</summary>
}
