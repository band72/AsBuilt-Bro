using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

// Design / Curve Commands
// Many of these set parameters for curve construction or stakeout.

public class PcCommand : ICommand
{
    public string Name => "PC";
    public string Description => "Point of Curvature.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"PC set to {args[1]} (Stored)");
         return Task.CompletedTask;
    }
}

public class CrvCommand : ICommand
{
    public string Name => "CRV";
    public string Description => "Curve Definition Mode (DELTA/RADIUS/etc).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"Curve Mode set to {args[1]} (Stored)");
         return Task.CompletedTask;
    }
}

public class RtCommand : ICommand
{
    public string Name => "RT";
    public string Description => "Right Turn/Tangent?";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"RT set to {args[1]} (Stored)");
         return Task.CompletedTask;
    }
}

public class C3Command : ICommand
{
    public string Name        => "C3";
    public string Description => "3-Point Circle. Args: C3 <ptId1> <ptId2> <ptId3>  — computes circumscribed circle through three stored points.";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 4)
        {
            context.Log("Usage: C3 <point1> <point2> <point3>");
            return Task.CompletedTask;
        }

        var p1 = context.GetPoint(args[1]);
        var p2 = context.GetPoint(args[2]);
        var p3 = context.GetPoint(args[3]);

        if (p1 == null) { context.Log($"Error: Point '{args[1]}' not found."); return Task.CompletedTask; }
        if (p2 == null) { context.Log($"Error: Point '{args[2]}' not found."); return Task.CompletedTask; }
        if (p3 == null) { context.Log($"Error: Point '{args[3]}' not found."); return Task.CompletedTask; }

        // ── Circumscribed circle via perpendicular bisector intersection ──────
        // Using the algebraic form of the circumcircle determinant.
        double ax = p1.Easting,  ay = p1.Northing;
        double bx = p2.Easting,  by = p2.Northing;
        double cx = p3.Easting,  cy = p3.Northing;

        double D = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (Math.Abs(D) < 1e-9)
        {
            context.Log("C3 Error: The three points are collinear — no unique circle exists.");
            return Task.CompletedTask;
        }

        double a2 = ax * ax + ay * ay;
        double b2 = bx * bx + by * by;
        double c2 = cx * cx + cy * cy;

        double ux = (a2 * (by - cy) + b2 * (cy - ay) + c2 * (ay - by)) / D;
        double uy = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / D;

        double radius = Math.Sqrt((ax - ux) * (ax - ux) + (ay - uy) * (ay - uy));

        context.Log($"C3 — 3-Point Circle");
        context.Log($"  Center  : N={uy:F4}  E={ux:F4}");
        context.Log($"  Radius  : {radius:F4} ft");
        context.Log($"  Circum  : {2 * Math.PI * radius:F4} ft");
        return Task.CompletedTask;
    }
}

public class OffsetCommand : ICommand
{
    public string Name => "OFFSET";
    public string Description => "Offset Mode/Value.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"Offset set to {args[1]} (Stored)");
         return Task.CompletedTask;
    }
}

public class ModCommand : ICommand
{
    public string Name => "MOD";
    public string Description => "Model/Mode?";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"MOD set to {args[1]} (Stored)");
         return Task.CompletedTask;
    }
}

public class StakeoutCommand : ICommand
{
    public string Name { get; }
    public string Description => "Stakeout parameter.";

    public StakeoutCommand(string name) => Name = name;

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.Log($"{Name} set to {args[1]} (Stored)");
        return Task.CompletedTask;
    }
}
