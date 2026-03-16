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
    public string Description => "Closes Figure path back to start and then ends it.";
    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        var fig = context.CurrentFigure;
        if (fig != null && fig.PointIds.Count > 0)
        {
            var firstPt = fig.PointIds[0];
            var lastPt = fig.PointIds[fig.PointIds.Count - 1];
            // Only add the closing line if not already explicitly closed by the user
            if (firstPt != lastPt)
            {
                new ContCommand().ExecuteAsync(new string[] { "CONT", firstPt }, context);
            }
        }
        return new EndCommand().ExecuteAsync(args, context);
    }
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
    public string Description => "Alias for BEG (Begin Figure).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new BegCommand().ExecuteAsync(args, context);
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
    public string Description => "Alias for BEG (Begin Figure).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new BegCommand().ExecuteAsync(args, context);
}

public class ArcArcCommand : ICommand
{
    public string Name => "ARCARC";
    public string Description => "Alias for RKRK (Range-Range Intersect).";
    public Task ExecuteAsync(string[] args, ICogoContext context) => new RkRkCommand().ExecuteAsync(args, context);
}


