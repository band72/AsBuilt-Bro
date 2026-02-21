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
    public string Name => "C3";
    public string Description => "3-Point Curve?";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         context.Log("C3 Command executed (Placeholder).");
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
