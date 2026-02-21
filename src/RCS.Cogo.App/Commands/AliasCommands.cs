using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class StartCommand : ICommand
{
    public string Name => "START";
    public string Description => "Alias for BEG (Begin Figure).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new BegCommand().ExecuteAsync(args, context);
}

public class PointCommand : ICommand
{
    public string Name => "POINT";
    public string Description => "Alias for CONT (Add Point).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new ContCommand().ExecuteAsync(args, context);
}

public class CloseCommand : ICommand
{
    public string Name => "CLOSE";
    public string Description => "Ends Figure (and implies closure). Alias for END.";
    // For now, mapping to END. Real CLOSE might draw a line to start, but END just stops defining.
    // If the figure needs to be 'Closed', ContCommand logic or MapCheck logic handles the geometry.
    public Task ExecuteAsync(string[] args, ICogoContext context) => new EndCommand().ExecuteAsync(args, context);
}

public class InverseCommand : ICommand
{
    public string Name => "INVERSE";
    public string Description => "Alias for INV.";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new InvCommand().ExecuteAsync(args, context);
}

// Additional Aliases
public class FigCommand : ICommand
{
    public string Name => "FIG";
    public string Description => "Alias for CONT (Add Point to Figure).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new ContCommand().ExecuteAsync(args, context);
}

public class PtCommand : ICommand
{
    public string Name => "PT";
    public string Description => "Alias for NEZ (Add Point).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new NezCommand().ExecuteAsync(args, context);
}

public class ACommand : ICommand
{
    public string Name => "A";
    public string Description => "Alias for AZ (Azimuth).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new AzCommand().ExecuteAsync(args, context);
}

public class DCommand : ICommand
{
    public string Name => "D";
    public string Description => "Alias for DIST (Distance).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new DistCommand().ExecuteAsync(args, context);
}

public class BCommand : ICommand
{
    public string Name => "B";
    public string Description => "Display Bearing (Inverse). Maps to INV.";
    // Likely maps to Inverse or Bearing info. Let's map to INV as safe bet for info display.
    public Task ExecuteAsync(string[] args, ICogoContext context) => new InvCommand().ExecuteAsync(args, context);
}

public class ArcArcCommand : ICommand
{
    public string Name => "ARCARC";
    public string Description => "Alias for RKRK (Range-Range Intersect).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new RkRkCommand().ExecuteAsync(args, context);
}


