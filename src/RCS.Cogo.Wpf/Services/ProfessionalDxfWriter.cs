using RCS.Cogo.Core.Primitives;
using System.Text;
using System.Collections.Generic;
using System.IO;

namespace RCS.Cogo.Wpf.Services;

public class ProfessionalDxfWriter
{
    private readonly StringBuilder _sb = new();
    private int _handleCounter = 0x100;

    private string NextHandle() => (++_handleCounter).ToString("X");
    
    // Header
    public void Begin()
    {
        _sb.AppendLine("0");
        _sb.AppendLine("SECTION");
        _sb.AppendLine("2");
        _sb.AppendLine("HEADER");
        _sb.AppendLine("9");
        _sb.AppendLine("$ACADVER");
        _sb.AppendLine("1");
        _sb.AppendLine("AC1009");
        _sb.AppendLine("0");
        _sb.AppendLine("ENDSEC");
        
        // Symbols as Blocks? For now simple geometry, but professional DXF uses blocks.
        // Will implement BLOCK definitions for symbols here.
        DefineBlocks();
        
        _sb.AppendLine("0");
        _sb.AppendLine("SECTION");
        _sb.AppendLine("2");
        _sb.AppendLine("ENTITIES");
    }
    
    // Blocks Section
    private void DefineBlocks()
    {
        _sb.AppendLine("0");
        _sb.AppendLine("SECTION");
        _sb.AppendLine("2");
        _sb.AppendLine("BLOCKS");
        
        // 1. Manhole Block (Circle with M)
        DefineBlock("MANHOLE", () => 
        {
            AddCircle(0,0, 1.0, "0"); // Radius 1
            AddText("M", 0,0, 0.8, "0", "CENTER");
        });
        
        // 2. Valve Block (Bowtie)
        DefineBlock("VALVE", () => 
        {
            // Bowtie: (0,0)->(1,1) & (1,0)->(0,1) with lines closing
            // Actually usually two triangles meeting at point.
            // (-0.5, -0.5) to (0.5, 0.5)
            AddLine(-0.5, -0.5, 0.5, 0.5, "0");
            AddLine(-0.5, 0.5, 0.5, -0.5, "0");
            AddLine(-0.5, -0.5, -0.5, 0.5, "0"); // Left side close
            AddLine(0.5, -0.5, 0.5, 0.5, "0");   // Right side close
        });
        
        // 3. Hydrant Block (Circle with Cross)
        DefineBlock("HYDRANT", () => 
        {
            AddCircle(0,0, 0.8, "0"); 
            AddLine(-0.8, 0, 0.8, 0, "0");
            AddLine(0, -0.8, 0, 0.8, "0");
        });
        
        _sb.AppendLine("0");
        _sb.AppendLine("ENDSEC");
    }
    
    private void DefineBlock(string name, System.Action content)
    {
        _sb.AppendLine("0");
        _sb.AppendLine("BLOCK");
        _sb.AppendLine("8"); // Layer
        _sb.AppendLine("0"); // Layer 0
        _sb.AppendLine("2"); // Block Name
        _sb.AppendLine(name);
        _sb.AppendLine("70"); // Flags
        _sb.AppendLine("0");
        _sb.AppendLine("10"); // Base Point X
        _sb.AppendLine("0.0");
        _sb.AppendLine("20"); // Base Point Y
        _sb.AppendLine("0.0");
        _sb.AppendLine("30"); // Base Point Z
        _sb.AppendLine("0.0");
        
        content();
        
        _sb.AppendLine("0");
        _sb.AppendLine("ENDBLK");
    }

    // Entities
    public void AddPoint(Point3D p, string layer = "POINTS")
    {
        _sb.AppendLine("0");
        _sb.AppendLine("POINT");
        _sb.AppendLine("8");
        _sb.AppendLine(layer);
        _sb.AppendLine("10");
        _sb.AppendLine(p.Easting.ToString("F4"));
        _sb.AppendLine("20");
        _sb.AppendLine(p.Northing.ToString("F4"));
        _sb.AppendLine("30");
        _sb.AppendLine(p.Elevation.ToString("F4"));
    }
    
    public void AddLine(double x1, double y1, double x2, double y2, string layer)
    {
        _sb.AppendLine("0");
        _sb.AppendLine("LINE");
        _sb.AppendLine("8");
        _sb.AppendLine(layer);
        _sb.AppendLine("10");
        _sb.AppendLine(x1.ToString("F4"));
        _sb.AppendLine("20");
        _sb.AppendLine(y1.ToString("F4"));
        _sb.AppendLine("11");
        _sb.AppendLine(x2.ToString("F4"));
        _sb.AppendLine("21");
        _sb.AppendLine(y2.ToString("F4"));
    }
    
    public void AddCircle(double x, double y, double r, string layer)
    {
        _sb.AppendLine("0");
        _sb.AppendLine("CIRCLE");
        _sb.AppendLine("8");
        _sb.AppendLine(layer);
        _sb.AppendLine("10");
        _sb.AppendLine(x.ToString("F4"));
        _sb.AppendLine("20");
        _sb.AppendLine(y.ToString("F4"));
        _sb.AppendLine("40");
        _sb.AppendLine(r.ToString("F4"));
    }
    
    public void AddText(string text, double x, double y, double height, string layer, string align = "LEFT")
    {
        _sb.AppendLine("0");
        _sb.AppendLine("TEXT");
        _sb.AppendLine("8");
        _sb.AppendLine(layer);
        _sb.AppendLine("10");
        _sb.AppendLine(x.ToString("F4"));
        _sb.AppendLine("20");
        _sb.AppendLine(y.ToString("F4"));
        _sb.AppendLine("40");
        _sb.AppendLine(height.ToString("F4"));
        _sb.AppendLine("1");
        _sb.AppendLine(text);
        // Additional align logic if needed
    }
    
    public void InsertBlock(string blockName, double x, double y, double scale, string layer)
    {
        _sb.AppendLine("0");
        _sb.AppendLine("INSERT");
        _sb.AppendLine("8");
        _sb.AppendLine(layer);
        _sb.AppendLine("2");
        _sb.AppendLine(blockName);
        _sb.AppendLine("10");
        _sb.AppendLine(x.ToString("F4"));
        _sb.AppendLine("20");
        _sb.AppendLine(y.ToString("F4"));
        _sb.AppendLine("41"); // X Scale
        _sb.AppendLine(scale.ToString("F4"));
        _sb.AppendLine("42"); // Y Scale
        _sb.AppendLine(scale.ToString("F4"));
        _sb.AppendLine("43"); // Z Scale
        _sb.AppendLine(scale.ToString("F4"));
    }

    public void End()
    {
        _sb.AppendLine("0");
        _sb.AppendLine("ENDSEC");
        _sb.AppendLine("0");
        _sb.AppendLine("EOF");
    }
    
    public void Save(string path)
    {
        File.WriteAllText(path, _sb.ToString());
    }
}
