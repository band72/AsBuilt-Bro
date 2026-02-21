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
        if (args.Length < 2)
        {
            context.Log("Error: Usage: SAVE <Filename>");
            return Task.CompletedTask;
        }

        string filename = args[1];
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
