using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class UnitsCommand : ICommand
{
    public string Name => "UNITS";
    public string Description => "Set distance units (FOOT/METER).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.Units = args[1].ToUpper();
        context.Log($"Units: {context.Units}");
        return Task.CompletedTask;
    }
}

public class AtmosCommand : ICommand
{
    public string Name => "ATMOS";
    public string Description => "Toggle Atmospheric Correction (ON/OFF).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.AtmosCorrection = args[1].ToUpper() == "ON";
        context.Log($"Atmos Correction: {(context.AtmosCorrection ? "ON" : "OFF")}");
        return Task.CompletedTask;
    }
}

public class TempCommand : ICommand
{
    public string Name => "TEMP";
    public string Description => "Set Temperature.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1 && double.TryParse(args[1], out double val)) context.Temperature = val;
        context.Log($"Temperature: {context.Temperature}");
        return Task.CompletedTask;
    }
}

public class PressCommand : ICommand
{
    public string Name => "PRESS";
    public string Description => "Set Pressure.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1 && double.TryParse(args[1], out double val)) context.Pressure = val;
        context.Log($"Pressure: {context.Pressure}");
        return Task.CompletedTask;
    }
}

public class SfCommand : ICommand
{
    public string Name => "SF";
    public string Description => "Set Scale Factor.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1 && double.TryParse(args[1], out double val)) context.ScaleFactor = val;
        context.Log($"Scale Factor: {context.ScaleFactor:F6}");
        return Task.CompletedTask;
    }
}

public class CrCommand : ICommand
{
    public string Name => "CR";
    public string Description => "Toggle Curvature & Refraction (ON/OFF).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.CurvatureRefraction = args[1].ToUpper() == "ON";
        context.Log($"C & R: {(context.CurvatureRefraction ? "ON" : "OFF")}");
        return Task.CompletedTask;
    }
}

public class AnglesCommand : ICommand
{
    public string Name => "ANGLES";
    public string Description => "Set Angle Mode (RIGHT/LEFT).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.AngleFormat = args[1].ToUpper();
        context.Log($"Angle Mode: {context.AngleFormat}");
        return Task.CompletedTask;
    }
}

public class VertCommand : ICommand
{
    public string Name => "VERT";
    public string Description => "Set Vertical Angle Mode (ZENITH/HORIZ).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.VerticalFormat = args[1].ToUpper();
        context.Log($"Vertical Mode: {context.VerticalFormat}");
        return Task.CompletedTask;
    }
}

public class HorizCommand : ICommand
{
    public string Name => "HORIZ";
    public string Description => "Set Horizontal Mode (ANGLE/BEARING/AZIMUTH).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
       // Just storage for now
       if (args.Length > 1) context.Log($"Horizontal Mode set to {args[1]} (Stored)");
       return Task.CompletedTask;
    }
}

public class EdmCommand : ICommand
{
    public string Name => "EDM";
    public string Description => "Set EDM Mode.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.EdmMode = args[1].ToUpper();
        context.Log($"EDM Mode: {context.EdmMode}");
        return Task.CompletedTask;
    }
}

public class PrismCommand : ICommand
{
    public string Name => "PRISM";
    public string Description => "Set Prism Constant/Mode.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) context.PrismMode = args[1].ToUpper();
        context.Log($"Prism: {context.PrismMode}");
        return Task.CompletedTask;
    }
}

public class CollCommand : ICommand
{
    public string Name => "COLL";
    public string Description => "Toggle Collimation (ON/OFF).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // Just log for now
        context.Log($"Collimation: {(args.Length > 1 ? args[1] : "Info")}");
        return Task.CompletedTask;
    }
}

public class ApCommand : ICommand
{
    public string Name => "AP";
    public string Description => "Toggle Auto Point mode (ON/OFF).";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length > 1) 
        {
            context.AutoPoint = args[1].ToUpper() == "ON";
        }
        else
        {
            context.AutoPoint = !context.AutoPoint;
        }
        
        context.Log($"AP set to {(context.AutoPoint ? "ON" : "OFF")} (Stored)");
        return Task.CompletedTask;
    }
}

public class ResetConfigCommand : ICommand
{
    public string Name { get; }
    public string Description => "Toggles viewport scripts reset behavior (RESET-ON/RESET-OFF).";

    public ResetConfigCommand(string name)
    {
        Name = name;
    }

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (Name == "RESET-ON")
            context.Log("[INFO] Next script run WILL clear viewport.");
        else if (Name == "RESET-OFF")
            context.Log("[INFO] Next script run will NOT clear viewport.");
            
        return Task.CompletedTask;
    }
}
