using System;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class AzCommand : ICommand
{
    public string Name => "AZ";
    public string Description => "Calculate Azimuth between two points. Usage: AZ <Pt1> <Pt2>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 3) return Task.CompletedTask;
        var p1 = context.GetPoint(args[1]);
        var p2 = context.GetPoint(args[2]);
        if (p1 == null || p2 == null) { context.Log("Point not found."); return Task.CompletedTask; }
        
        var res = GeometryEngine.Inverse(p1, p2);
        context.Log($"Azimuth {args[1]}-{args[2]}: {res.Azimuth.ToDMS():F4}");
        return Task.CompletedTask;
    }
}

public class DistCommand : ICommand
{
    public string Name => "DIST"; // Or D
    public string Description => "Calculate Distance between two points. Usage: DIST <Pt1> <Pt2>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 3) return Task.CompletedTask;
        var p1 = context.GetPoint(args[1]);
        var p2 = context.GetPoint(args[2]);
        if (p1 == null || p2 == null) { context.Log("Point not found."); return Task.CompletedTask; }
        
        var res = GeometryEngine.Inverse(p1, p2);
        context.Log($"Distance {args[1]}-{args[2]}: {res.Distance:F4}");
        return Task.CompletedTask;
    }
}

public class AngCommand : ICommand
{
    public string Name => "ANG"; // Or A
    public string Description => "Calculate Angle at Pt2, from Pt1 to Pt3. Usage: ANG <Pt1> <Pt2> <Pt3>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 4) 
        {
            context.Log("Usage: ANG <BackPt> <AtPt> <ForePt>");
            return Task.CompletedTask;
        }
        
        var p1 = context.GetPoint(args[1]);
        var p2 = context.GetPoint(args[2]);
        var p3 = context.GetPoint(args[3]);
        
        if (p1 == null || p2 == null || p3 == null) { context.Log("Point not found."); return Task.CompletedTask; }
        
        // Azimuth 2->1 (Back)
        var inv1 = GeometryEngine.Inverse(p2, p1);
        // Azimuth 2->3 (Fore)
        var inv2 = GeometryEngine.Inverse(p2, p3);
        
        // Angle Right = AzFore - AzBack
        double angRad = inv2.Azimuth.Radians - inv1.Azimuth.Radians;
        if (angRad < 0) angRad += 2 * Math.PI;
        
        var angle = Angle.FromRadians(angRad);
        
        context.Log($"Angle {args[1]}-{args[2]}-{args[3]}: {angle.ToDMS():F4}");
        return Task.CompletedTask;
    }
}
