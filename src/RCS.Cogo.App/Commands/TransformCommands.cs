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
    public string Description => "Inverse of Line. Usage: LN <Pt1> <Pt2>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // LN <Pt1> <Pt2>
        if (args.Length < 3)
        {
            context.Log("Usage: LN <Pt1> <Pt2>");
            return Task.CompletedTask;
        }

        string p1 = args[1];
        string p2 = args[2];

        var pt1 = context.GetPoint(p1);
        var pt2 = context.GetPoint(p2);

        if (pt1 == null || pt2 == null)
        {
            context.Log("Error: One or both points not found.");
            return Task.CompletedTask;
        }

        var result = GeometryEngine.Inverse(pt1, pt2);
        
        context.Log($"LN {p1}-{p2}: Az {result.Azimuth.ToDMS():F4}  Dist: {result.Distance:F4} {context.Units}");

        return Task.CompletedTask;
    }
}

public class TrnCommand : ICommand
{
    public string Name => "TRN";
    public string Description => "Translate Points. Usage: TRN <SourcePt> <DestPt> <PtsToMoveRange>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // TRN <Source> <Dest> <PtsToMoveRange>
        if (args.Length < 4)
        {
            context.Log("Error: Usage: TRN <SourcePt> <DestPt> <PtsToMoveRange>");
            return Task.CompletedTask;
        }

        string srcId = args[1];
        string destId = args[2];
        string ptsRange = string.Join(",", args.Skip(3));

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
        var idsToMove = ParsePointRange(ptsRange, context);

        foreach (var ptId in idsToMove)
        {
            var pt = context.GetPoint(ptId);
            if (pt != null)
            {
                var newPt = new Point3D(pt.Northing + dy, pt.Easting + dx, pt.Elevation + dz);
                var oldDesc = context.GetAllPoints().FirstOrDefault(x => x.Id.Equals(ptId, StringComparison.OrdinalIgnoreCase)).Description ?? "";
                context.AddPoint(ptId, newPt, oldDesc);
                count++;
            }
        }
        
        context.Log($"Translated {count} points by dN:{dy:F3}, dE:{dx:F3}.");
        return Task.CompletedTask;
    }

    public static System.Collections.Generic.List<string> ParsePointRange(string rangeStr, ICogoContext context)
    {
        var ids = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parts = rangeStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.Contains("-"))
            {
                var ends = part.Split('-');
                if (ends.Length == 2 && int.TryParse(ends[0], out int start) && int.TryParse(ends[1], out int end))
                {
                    if (start > end)
                    {
                        int temp = start; start = end; end = temp;
                    }
                    for (int i = start; i <= end; i++) ids.Add(i.ToString());
                }
                else
                {
                    ids.Add(part.Trim());
                }
            }
            else
            {
                ids.Add(part.Trim());
            }
        }
        return ids.ToList();
    }
}

public class RotCommand : ICommand
{
    public string Name => "ROT";
    public string Description => "Rotate Points. Usage: ROT <Ln1.Start-Ln1.End> <Ln2.Start-Ln2.End> <PtsRange>"; 
    
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 4)
        {
            context.Log("Usage: ROT <Ln1Start-Ln1End> <Ln2Start-Ln2End> <PtsRange>"); 
            return Task.CompletedTask;
        }

        string ln1Str = args[1];
        string ln2Str = args[2];
        string ptsRange = string.Join(",", args.Skip(3));
        
        if (!ln1Str.Contains("-") || !ln2Str.Contains("-"))
        {
            context.Log("Error: Line formats must be 'PtA-PtB' (e.g. 1-2).");
            return Task.CompletedTask;
        }

        var ln1Pts = ln1Str.Split('-');
        var ln2Pts = ln2Str.Split('-');

        var ln1P1 = context.GetPoint(ln1Pts[0]);
        var ln1P2 = context.GetPoint(ln1Pts[1]);
        var ln2P1 = context.GetPoint(ln2Pts[0]);
        var ln2P2 = context.GetPoint(ln2Pts[1]);

        if (ln1P1 == null || ln1P2 == null || ln2P1 == null || ln2P2 == null)
        {
            context.Log($"Error: One or more points defining the lines '{ln1Str}' or '{ln2Str}' were not found.");
            return Task.CompletedTask;
        }

        var inv1 = GeometryEngine.Inverse(ln1P1, ln1P2);
        var inv2 = GeometryEngine.Inverse(ln2P1, ln2P2);
        
        double diffRadians = inv2.Azimuth.Radians - inv1.Azimuth.Radians;
        var center = ln1P1;

        int count = 0;
        var idsToRotate = TrnCommand.ParsePointRange(ptsRange, context);

        foreach (var id in idsToRotate)
        {
            var p = context.GetPoint(id);
            if (p != null)
            {
                var invToCenter = GeometryEngine.Inverse(center, p);
                
                double rotRadius = invToCenter.Distance;
                double newAzimuth = invToCenter.Azimuth.Radians + diffRadians;
                
                var newPt = GeometryEngine.Forward(center, Angle.FromRadians(newAzimuth), rotRadius);

                newPt = new Point3D(newPt.Northing, newPt.Easting, p.Elevation);
                var oldDesc = context.GetAllPoints().FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase)).Description ?? "";
                
                context.AddPoint(id, newPt, oldDesc);
                count++;
            }
        }
        
        context.Log($"Rotated {count} points around {ln1Pts[0]} by angle difference.");
        return Task.CompletedTask;
    }
}
