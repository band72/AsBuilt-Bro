using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;
using RCS.Cogo.Core.Primitives;

namespace RCS.Cogo.App.Commands;

public class CopyPtCommand : ICommand
{
    private readonly string _name;

    public CopyPtCommand(string name = "COPY-PT")
    {
        _name = name;
    }

    public string Name => _name;
    public string Description => $"Copy a point to a new point number with an optional description. Usage: {Name} <OldPt> <NewPt> [Desc]";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 3)
        {
            context.Log("Error: Usage: COPY-PT <OldPt> <NewPt> [Desc]");
            return Task.CompletedTask;
        }

        string oldPtId = args[1];
        string newPtId = args[2];
        
        var allPoints = context.GetAllPoints();
        var oldPtData = allPoints.FirstOrDefault(p => p.Id == oldPtId);

        if (oldPtData.Point == null)
        {
            context.Log($"Error: Source point {oldPtId} not found.");
            return Task.CompletedTask;
        }

        string newDesc = args.Length > 3 ? string.Join(" ", args.Skip(3)).Trim('"') : oldPtData.Description;

        context.AddPoint(newPtId, new Point3D(oldPtData.Point.Northing, oldPtData.Point.Easting, oldPtData.Point.Elevation), newDesc);
        context.Log($"Copied Point {oldPtId} to {newPtId}");

        return Task.CompletedTask;
    }
}
