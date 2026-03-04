using System.Linq;
using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class XptPtsCommand : ICommand
{
    public string Name => "XPT-PTS";
    public string Description => "Create list of all points in the COGO database. Usage: XPT-PTS";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        var allPoints = context.GetAllPoints()
            .OrderBy(p => int.TryParse(p.Id, out int idVal) ? idVal : int.MaxValue)
            .ThenBy(p => p.Id);
            
        foreach (var pointRef in allPoints)
        {
            var id = pointRef.Id;
            var pt = pointRef.Point;
            var desc = pointRef.Description;
            string descStr = string.IsNullOrWhiteSpace(desc) ? "" : $" {desc}";
            context.Log($"NEZ {id} {pt.Northing:F3} {pt.Easting:F3} {pt.Elevation:F3}{descStr}");
        }

        return Task.CompletedTask;
    }
}
