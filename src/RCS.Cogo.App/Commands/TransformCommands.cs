using System;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.App.State; // Added for Figure
using RCS.Cogo.Core.Primitives;
using RCS.Cogo.Core.Maths;

namespace RCS.Cogo.App.Commands;

public class LnCommand : ICommand
{
    public string Name => "LN";
    public string Description => "Create a Line (Figure) between two points. Usage: LN <Pt1> <Pt2> <Name>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // LN <Pt1> <Pt2> <Name>
        if (args.Length < 4)
        {
            context.Log("Usage: LN <Pt1> <Pt2> <Name>");
            return Task.CompletedTask;
        }

        string p1 = args[1];
        string p2 = args[2];
        string name = args[3];

        if (context.GetPoint(p1) == null || context.GetPoint(p2) == null)
        {
            context.Log("Error: One or both points not found.");
            return Task.CompletedTask;
        }

        var fig = new Figure(name);
        fig.AddPoint(p1);
        fig.AddPoint(p2);
        context.AddFigure(fig);
        
        context.Log($"Line {name} created from {p1} to {p2}.");
        return Task.CompletedTask;
    }
}

public class TrnCommand : ICommand
{
    public string Name => "TRN";
    public string Description => "Translate Points. Usage: TRN <SourcePt> <DestPt> <Pt1> [Pt2...]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // TRN <Source> <Dest> <PtToMove...>
        if (args.Length < 4)
        {
            context.Log("Error: Usage: TRN <SourcePt> <DestPt> <PtToMove...>");
            return Task.CompletedTask;
        }

        string srcId = args[1];
        string destId = args[2];

        var pSrc = context.GetPoint(srcId);
        var pDest = context.GetPoint(destId);

        if (pSrc == null || pDest == null)
        {
            context.Log("Error: Source or Destination point not found.");
            return Task.CompletedTask;
        }

        double dx = pDest.Easting - pSrc.Easting;
        double dy = pDest.Northing - pSrc.Northing;
        double dz = pDest.Elevation - pSrc.Elevation;

        int count = 0;
        for (int i = 3; i < args.Length; i++)
        {
            string ptId = args[i];
            var pt = context.GetPoint(ptId);
            if (pt != null)
            {
                var newPt = new Point3D(pt.Northing + dy, pt.Easting + dx, pt.Elevation + dz);
                // Update point in context (AddPoint overwrites if ID exists)
                context.AddPoint(ptId, newPt, "Translated");
                count++;
            }
            else
            {
                context.Log($"Warning: Point {ptId} not found.");
            }
        }
        
        context.Log($"Translated {count} points by dN:{dy:F3}, dE:{dx:F3}.");
        return Task.CompletedTask;
    }
}

public class RotCommand : ICommand
{
    public string Name => "ROT";
    public string Description => "Rotate Points. Usage: ROT <SourcePt> <DestPt> <Pt1> [Pt2...]"; 
    
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 4)
        {
            context.Log("Usage: ROT <SourceLn> <DestLn> <Points...>"); 
            return Task.CompletedTask;
        }

        string srcLnName = args[1];
        string destLnName = args[2];
        
        // Get Angles of lines
        double angSrc = GetLineAzimuth(srcLnName, context);
        double angDest = GetLineAzimuth(destLnName, context);
        
        if (double.IsNaN(angSrc) || double.IsNaN(angDest))
        {
            context.Log("Error: Could not determine azimuths for source/dest lines.");
            return Task.CompletedTask;
        }
        
        double rotation = angDest - angSrc; // Angle to rotate RIGHT
        
        // Origin? 
        // Rotation requires a pivot. 
        // If we just rotate points, they rotate around (0,0)? Unlikely.
        // Usually around the start of the source line?
        // Let's assume pivot is Start Point of Source Line.
        
        var srcFig = context.GetFigure(srcLnName);
        Point3D pivot = new Point3D(0,0,0);
        if (srcFig != null && srcFig.PointIds.Count > 0)
        {
            if (context.GetPoint(srcFig.PointIds[0]) != null)
            {
                pivot = context.GetPoint(srcFig.PointIds[0])!;
            }
        }

        double rad = Angle.FromDegrees(rotation).Radians;
        double s = Math.Sin(rad);
        double c = Math.Cos(rad);
        
        int count = 0;
        for (int i = 3; i < args.Length; i++)
        {
            string ptId = args[i];
            var pt = context.GetPoint(ptId);
            if (pt != null)
            {
                // Valid Point
                double dx = pt.Easting - pivot.Easting;
                double dy = pt.Northing - pivot.Northing;
                
                double xNew = (dx * c) - (dy * s) + pivot.Easting;
                double yNew = (dx * s) + (dy * c) + pivot.Northing;
                
                context.AddPoint(ptId, new Point3D(yNew, xNew, pt.Elevation), "Rotated");
                count++;
            }
        }
        
        context.Log($"Rotated {count} points by {rotation:F4} deg around {pivot.Easting:F3},{pivot.Northing:F3}.");
        return Task.CompletedTask;
    }
    
    private double GetLineAzimuth(string lineName, ICogoContext context)
    {
        // Try Figure
        var fig = context.GetFigure(lineName);
        if (fig != null && fig.PointIds.Count >= 2)
        {
            var p1 = context.GetPoint(fig.PointIds[0]);
            var p2 = context.GetPoint(fig.PointIds[1]);
            if (p1 != null && p2 != null)
            {
                // Use Inverse to get Azimuth
                var inverse = GeometryEngine.Inverse(p1, p2);
                return inverse.Azimuth.Degrees;
            }
        }
        return double.NaN;
    }
}
