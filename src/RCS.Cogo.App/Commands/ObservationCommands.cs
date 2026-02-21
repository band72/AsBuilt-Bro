using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class FaceCommand : ICommand
{
    public string Name { get; }
    public string Description => "Face 1/2 Observation (Angle Right/Dist). Usage: F1/F2 <Pt> <Ang> <Dist>";

    public FaceCommand(string name)
    {
        Name = name;
    }

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // F1 <Pt> <Ang> <Dist>
        // Treats as Angle Right from Backsight. Same as AD.
        // We delegate to AD logic.
        // We'll just create a new AdCommand instance or reuse logic.
        // But AD command expects "AD" as first arg? No, ExecuteAsync takes args.
        // If we reuse AD, we need to make sure args[0] doesn't matter or we patch it.
        // AdCommand likely ignores args[0] except for validation?
        
        // Let's just reimplement simple logic here to avoid coupling or args hacking.
        
        if (context.CurrentStation == null)
        {
            context.Log("Error: No station set.");
            return Task.CompletedTask;
        }
        if (context.CurrentBacksight == null)
        {
            context.Log("Error: No backsight set.");
            return Task.CompletedTask;
        }

        if (args.Length < 4)
        {
            context.Log($"Usage: {Name} <Pt> <Ang> <Dist>");
            return Task.CompletedTask;
        }
        
        string ptId = args[1];
        if (!double.TryParse(args[2], out double angDms) || !double.TryParse(args[3], out double dist))
        {
            context.Log("Error: Invalid Angle/Dist.");
            return Task.CompletedTask;
        }
        
        // Calculate Azimuth = BackAz + Angle
        var inv = GeometryEngine.Inverse(context.CurrentStation, context.CurrentBacksight);
        var backAz = inv.Azimuth;
        var turnAng = Angle.FromDMS(angDms);
        var newAz = backAz + turnAng;
        
        var newPt = GeometryEngine.Forward(context.CurrentStation, newAz, dist);
        
        string desc = args.Length > 4 ? args[4] : "";
        context.AddPoint(ptId, newPt, desc);
        context.Log($"Point {ptId} created: {newPt}"); // Simplify log
        
        // Traverse Mode?
        if (context.TraverseMode)
        {
             context.CurrentBacksight = context.CurrentStation;
             context.CurrentStation = newPt;
             context.Log($"Auto-Traverse: STN={ptId}, BS={context.CurrentBacksight}"); // Need ID for BS?
        }
        
        return Task.CompletedTask;
    }
}

public class DdCommand : ICommand
{
    public string Name => "DD";
    public string Description => "Deflection Distance. Usage: DD <Pt> <Defl> <Dist>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (context.CurrentStation == null || context.CurrentBacksight == null)
        {
             context.Log("Error: Station/Backsight required.");
             return Task.CompletedTask;
        }
        
        if (args.Length < 4)
        {
            context.Log("Usage: DD <Pt> <Defl> <Dist>");
            return Task.CompletedTask;
        }
        
        string ptId = args[1];
        if (!double.TryParse(args[2], out double deflDms) || !double.TryParse(args[3], out double dist))
        {
             context.Log("Error: Invalid Defl/Dist.");
             return Task.CompletedTask;
        }
        
        // Deflection is from Prolongation of Backsight line.
        // Az(BS->STN) = BackAz + 180 (or just Inverse BS->STN)
        var inv = GeometryEngine.Inverse(context.CurrentBacksight, context.CurrentStation);
        var forwardAz = inv.Azimuth; // Azimuth from BS to STN. Prolongation is this same Azimuth.
        // Wait.
        // Backsight Point is B. Station is S.
        // We are at S, looking at B.
        // Angle Right 0 is looking at B.
        // Deflection 0 is looking away from B (Extension of line B->S).
        // Az(B->S) is the direction of the line B->S.
        
        var deflAng = Angle.FromDMS(deflDms);
        var finalAz = forwardAz + deflAng;
        
        var newPt = GeometryEngine.Forward(context.CurrentStation, finalAz, dist);
        
        string desc = args.Length > 4 ? args[4] : "";
        context.AddPoint(ptId, newPt, desc);
        context.Log($"Point {ptId} by Deflection created: {newPt}");
        
        if (context.TraverseMode)
        {
            context.CurrentBacksight = context.CurrentStation;
            context.CurrentStation = newPt;
            context.Log($"Traversed to new station: {ptId}");
        }

        return Task.CompletedTask;
    }
}
