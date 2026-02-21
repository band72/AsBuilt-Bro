using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Maths;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class AzAzCommand : ICommand
{
    public string Name => "AZAZ";
    public string Description => "Intersection by two Azimuths. Usage: AZAZ <PtNew> <Pt1> <Az1> <Pt2> <Az2> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 6)
        {
            context.Log("Error: Usage: AZAZ <PtNew> <Pt1> <Az1> <Pt2> <Az2> [Desc]");
            return Task.CompletedTask;
        }

        string newPt = args[1];
        string id1 = args[2];
        string id2 = args[4];

        var p1 = context.GetPoint(id1);
        var p2 = context.GetPoint(id2);

        if (p1 == null || p2 == null)
        {
            context.Log("Error: One or both reference points not found.");
            return Task.CompletedTask;
        }

        if (!double.TryParse(args[3], out double az1Dms) || !double.TryParse(args[5], out double az2Dms))
        {
            context.Log("Error: Invalid Azimuth format.");
            return Task.CompletedTask;
        }

        string desc = args.Length > 6 ? args[6] : "";

        var az1 = Angle.FromDMS(az1Dms);
        var az2 = Angle.FromDMS(az2Dms);

        var result = GeometryEngine.IntersectionBearingBearing(p1, az1, p2, az2);

        if (result != null)
        {
            context.AddPoint(newPt, result, desc);
            context.Log($"Point {newPt} created at intersection: {result}");
        }
        else
        {
            context.Log("Error: Parallel lines, no intersection.");
        }

        return Task.CompletedTask;
    }
}
