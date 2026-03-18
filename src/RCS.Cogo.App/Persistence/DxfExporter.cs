using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RCS.Cogo.Core.Primitives;
using RCS.Piping.Core.Models;

namespace RCS.Cogo.App.Persistence;

public static class DxfExporter
{
    private static string SanitizeLayerName(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "DEFAULT";
        var invalidChars = new[] { '<', '>', '/', '\\', '"', ':', ';', '?', '*', '|', '=', '`', ' ' };
        var result = input;
        foreach (var c in invalidChars)
        {
            result = result.Replace(c, '_');
        }
        return result.Length > 255 ? result.Substring(0, 255) : result;
    }

    public static void Export(
        string filePath, 
        IEnumerable<Point3D> points, 
        IEnumerable<RCS.Cogo.App.State.Figure> figures, 
        IEnumerable<PipeRun> pipeRuns, 
        IEnumerable<PipeStructure> pipeStructures,
        Func<string, Point3D?> getPoint)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);

        // Header
        writer.WriteLine("  0\nSECTION\n  2\nHEADER\n  9\n$ACADVER\n  1\nAC1009\n  0\nENDSEC");
        
        // Entities Section
        writer.WriteLine("  0\nSECTION\n  2\nENTITIES");

        // Points
        foreach(var p in points)
        {
            writer.WriteLine("  0\nPOINT");
            writer.WriteLine($"  8\nPOINTS");
            writer.WriteLine($" 10\n{p.Easting:F4}");
            writer.WriteLine($" 20\n{p.Northing:F4}");
            writer.WriteLine($" 30\n{p.Elevation:F4}");
        }

        // Generate Basic Polylines / Shapes for PipeStructures
        foreach (var structure in pipeStructures)
        {
            var p = getPoint(structure.PointId);
            if (p != null)
            {
                var type = structure.Type.ToUpperInvariant();
                double x = p.Easting;
                double y = p.Northing;
                double z = p.Elevation;
                string layer = SanitizeLayerName($"STRUCT_{structure.Type}");

                if (type.Contains("MANHOLE") || type.Contains("MH"))
                {
                    // Draw a Circle for Manhole (DXF Circle uses center 10,20,30 and radius 40)
                    writer.WriteLine("  0\nCIRCLE");
                    writer.WriteLine($"  8\n{layer}");
                    writer.WriteLine($" 10\n{x:F4}");
                    writer.WriteLine($" 20\n{y:F4}");
                    writer.WriteLine($" 30\n{z:F4}");
                    writer.WriteLine($" 40\n2.0"); // 2 foot radius manhole
                }
                else if (type.Contains("VALVE"))
                {
                     // Draw Triangle / Bowtie shape for valve (Lines)
                    writer.WriteLine("  0\nLINE");
                    writer.WriteLine($"  8\n{layer}");
                    writer.WriteLine($" 10\n{(x-1):F4}"); writer.WriteLine($" 20\n{(y-1):F4}"); writer.WriteLine($" 30\n{z:F4}");
                    writer.WriteLine($" 11\n{(x+1):F4}"); writer.WriteLine($" 21\n{(y+1):F4}"); writer.WriteLine($" 31\n{z:F4}");
                    writer.WriteLine("  0\nLINE");
                    writer.WriteLine($"  8\n{layer}");
                    writer.WriteLine($" 10\n{(x-1):F4}"); writer.WriteLine($" 20\n{(y+1):F4}"); writer.WriteLine($" 30\n{z:F4}");
                    writer.WriteLine($" 11\n{(x+1):F4}"); writer.WriteLine($" 21\n{(y-1):F4}"); writer.WriteLine($" 31\n{z:F4}");
                }
                else if (type.Contains("INLET") || type.Contains("METER"))
                {
                    // Draw a Square/Rectangle
                    double r = 1.5;
                    writer.WriteLine("  0\nLINE");
                    writer.WriteLine($"  8\n{layer}");
                    writer.WriteLine($" 10\n{(x-r):F4}"); writer.WriteLine($" 20\n{(y-r):F4}"); writer.WriteLine($" 30\n{z:F4}");
                    writer.WriteLine($" 11\n{(x+r):F4}"); writer.WriteLine($" 21\n{(y-r):F4}"); writer.WriteLine($" 31\n{z:F4}");
                    writer.WriteLine("  0\nLINE");
                    writer.WriteLine($"  8\n{layer}");
                    writer.WriteLine($" 10\n{(x+r):F4}"); writer.WriteLine($" 20\n{(y-r):F4}"); writer.WriteLine($" 30\n{z:F4}");
                    writer.WriteLine($" 11\n{(x+r):F4}"); writer.WriteLine($" 21\n{(y+r):F4}"); writer.WriteLine($" 31\n{z:F4}");
                    writer.WriteLine("  0\nLINE");
                    writer.WriteLine($"  8\n{layer}");
                    writer.WriteLine($" 10\n{(x+r):F4}"); writer.WriteLine($" 20\n{(y+r):F4}"); writer.WriteLine($" 30\n{z:F4}");
                    writer.WriteLine($" 11\n{(x-r):F4}"); writer.WriteLine($" 21\n{(y+r):F4}"); writer.WriteLine($" 31\n{z:F4}");
                    writer.WriteLine("  0\nLINE");
                    writer.WriteLine($"  8\n{layer}");
                    writer.WriteLine($" 10\n{(x-r):F4}"); writer.WriteLine($" 20\n{(y+r):F4}"); writer.WriteLine($" 30\n{z:F4}");
                    writer.WriteLine($" 11\n{(x-r):F4}"); writer.WriteLine($" 21\n{(y-r):F4}"); writer.WriteLine($" 31\n{z:F4}");
                }
                else 
                {
                    // Default Point
                    writer.WriteLine("  0\nPOINT");
                    writer.WriteLine($"  8\n{layer}");
                    writer.WriteLine($" 10\n{x:F4}");
                    writer.WriteLine($" 20\n{y:F4}");
                    writer.WriteLine($" 30\n{z:F4}");
                }
            }
        }

        // Figures (Lines)
        foreach(var fig in figures)
        {
            for(int i = 0; i < fig.PointIds.Count - 1; i++)
            {
                var p1 = getPoint(fig.PointIds[i]);
                var p2 = getPoint(fig.PointIds[i + 1]);
                if (p1 != null && p2 != null)
                {
                    writer.WriteLine("  0\nLINE");
                    writer.WriteLine($"  8\n{SanitizeLayerName(fig.Name)}");
                    writer.WriteLine($" 10\n{p1.Easting:F4}");
                    writer.WriteLine($" 20\n{p1.Northing:F4}");
                    writer.WriteLine($" 30\n{p1.Elevation:F4}");
                    writer.WriteLine($" 11\n{p2.Easting:F4}");
                    writer.WriteLine($" 21\n{p2.Northing:F4}");
                    writer.WriteLine($" 31\n{p2.Elevation:F4}");
                }
            }
        }

        // Pipe Runs (Lines)
        foreach(var run in pipeRuns)
        {
            var p1 = getPoint(run.FromPointId);
            var p2 = getPoint(run.ToPointId);
            if (p1 != null && p2 != null)
            {
                writer.WriteLine("  0\nLINE");
                writer.WriteLine($"  8\n{SanitizeLayerName($"UTILITY_{run.Type}")}");
                writer.WriteLine($" 10\n{p1.Easting:F4}");
                writer.WriteLine($" 20\n{p1.Northing:F4}");
                writer.WriteLine($" 30\n{(p1.Elevation - run.InvertStart):F4}");
                writer.WriteLine($" 11\n{p2.Easting:F4}");
                writer.WriteLine($" 21\n{p2.Northing:F4}");
                writer.WriteLine($" 31\n{(p2.Elevation - run.InvertEnd):F4}");
            }
        }

        // End Section and EOF
        writer.WriteLine("  0\nENDSEC\n  0\nEOF");
    }
}
