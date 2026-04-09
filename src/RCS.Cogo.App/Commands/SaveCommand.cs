using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class SaveCommand : ICommand
{
    public string Name => "SAVE";
    public string Description => "Saves the current project state to a .cogo file. Usage: SAVE <Filename>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        string filename = "";
        
        if (args.Length >= 2)
        {
            string param1 = args[1].ToUpper();
            if (param1 == "NORTH" || param1 == "SOUTH" || param1 == "EAST" || param1 == "WEST")
            {
                if (context.LastIntersections.Left == null || context.LastIntersections.Right == null)
                {
                    context.Log("Error: No active intersections stored. Run RKRK or other intersection command first.");
                    return Task.CompletedTask;
                }

                string ptId = context.AutoPoint ? context.GetNextPointId().ToString() : (args.Length >= 3 ? args[2] : "");
                if (string.IsNullOrEmpty(ptId))
                {
                    context.Log("Error: Usage: SAVE <Direction> <PtNew> or enable AP.");
                    return Task.CompletedTask;
                }

                var left = context.LastIntersections.Left;
                var right = context.LastIntersections.Right;

                RCS.Cogo.Core.Primitives.Point3D selectedPoint;
                
                if (param1 == "NORTH") selectedPoint = left.Northing > right.Northing ? left : right;
                else if (param1 == "SOUTH") selectedPoint = left.Northing < right.Northing ? left : right;
                else if (param1 == "EAST") selectedPoint = left.Easting > right.Easting ? left : right;
                else selectedPoint = left.Easting < right.Easting ? left : right; // WEST

                context.AddPoint(ptId, selectedPoint, $"RKRK Intersection ({param1})");
                context.Log($"Point {ptId} created: N:{selectedPoint.Northing:F4}, E:{selectedPoint.Easting:F4} (Saved {param1})");
                
                // Clear to prevent accidental reuse
                context.LastIntersections = (null, null);

                return Task.CompletedTask;
            }
            
            filename = args[1];
        }
        else
        {
            // Auto generation mode if no arguments provided
            string basePath = context.ProjectDirectory ?? "";
            
            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = RCS.Services.GlobalSettingsService.GetSetting("CogoScriptDefaultSavePath", System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
                string dateFolder = System.DateTime.Now.ToString("MMddyyyy.fff");
                basePath = System.IO.Path.Combine(basePath, dateFolder);
                if (!System.IO.Directory.Exists(basePath))
                    System.IO.Directory.CreateDirectory(basePath);
            }
            
            filename = System.IO.Path.Combine(basePath, $"AutoSave_{System.DateTime.Now:MMddyyyy_HHmmss}.cogo");
        }

        if (!filename.EndsWith(".cogo")) filename += ".cogo";

        var sb = new StringBuilder();
        sb.AppendLine("// RCS COGO Project Save File");
        
        // Save Points
        foreach (var p in context.GetAllPoints().OrderBy(x => x.Id))
        {
            // NE <Pt> <N> <E> "Desc"
            // Ensure Description is quoted if it has spaces?
            // Simple quote wrapping
            string desc = string.IsNullOrEmpty(p.Description) ? "" : $"\"{p.Description}\"";
            sb.AppendLine($"NE {p.Id} {p.Point.Northing:F4} {p.Point.Easting:F4} {desc}");
        }

        // Save Figures
        foreach (var fig in context.GetAllFigures())
        {
            sb.AppendLine($"BEG {fig.Name}");
            foreach (var pt in fig.PointIds)
            {
                sb.AppendLine($"CONT {pt}");
            }
            sb.AppendLine("END");
        }
        
        // Save Station/Backsight
        // Requires finding the Point ID associated with the coord
        // Simplification: We assume Points are static.
        
        if (context.CurrentStation != null)
        {
            var stn = context.GetAllPoints().FirstOrDefault(x => x.Point == context.CurrentStation);
             // Note: Record equality for Point3D works by value. So if point hasn't moved, it matches.
            if (stn.Id != null)
                sb.AppendLine($"STN {stn.Id}");
            else
                sb.AppendLine($"// Current Station at {context.CurrentStation} (Unlinked)");
        }
        
        if (context.CurrentBacksight != null)
        {
             var bs = context.GetAllPoints().FirstOrDefault(x => x.Point == context.CurrentBacksight);
             if (bs.Id != null)
                sb.AppendLine($"BS {bs.Id}");
        }

        try 
        {
            File.WriteAllText(filename, sb.ToString());
            context.Log($"Project saved to {Path.GetFullPath(filename)}");
        }
        catch (System.Exception ex)
        {
            context.Log($"Error saving file: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
