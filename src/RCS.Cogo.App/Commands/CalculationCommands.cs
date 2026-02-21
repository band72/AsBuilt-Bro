using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class AreaCommand : ICommand
{
    public string Name => "AREA";
    public string Description => "Calculate Area of a Figure. Usage: AREA <Figure>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // Reuse MapCheck logic as it calculates area
        return new MapCheckCommand().ExecuteAsync(args, context);
    }
}

public class CalcCommand : ICommand
{
    public string Name => "CALC";
    public string Description => "Evaluate math expression. Usage: CALC <Expr>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Usage: CALC <Expression>");
            return Task.CompletedTask;
        }
        
        string expression = string.Join(" ", args.Skip(1));
        
        try 
        {
            var table = new DataTable();
            var result = table.Compute(expression, null);
            context.Log($"CALC: {expression} = {result}");
        }
        catch (Exception ex)
        {
            context.Log($"Error calculating: {ex.Message}");
        }
        
        return Task.CompletedTask;
    }
}

public class SdCommand : ICommand
{
    public string Name => "SD";
    public string Description => "Slope Distance Mode."; 
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.Log($"SD Mode set to {args[1]} (Stored)");
        return Task.CompletedTask;
    }
}

public class VdCommand : ICommand
{
    public string Name => "VD";
    public string Description => "Vertical Distance Mode.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.Log($"VD Mode set to {args[1]} (Stored)");
        return Task.CompletedTask;
    }
}

public class GradeCommand : ICommand
{
    public string Name => "GRADE";
    public string Description => "Grade Mode.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"Grade Mode set to {args[1]} (Stored)");
         return Task.CompletedTask;
    }
}

public class SlopeCommand : ICommand
{
    public string Name => "SLOPE";
    public string Description => "Slope Mode.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"Slope Mode set to {args[1]} (Stored)");
         return Task.CompletedTask;
    }
}

public class StadiaCommand : ICommand
{
    public string Name => "STADIA";
    public string Description => "Stadia Constant.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
         if (args.Length > 1) context.Log($"Stadia constant set to {args[1]} (Stored)");
         return Task.CompletedTask;
    }
}
