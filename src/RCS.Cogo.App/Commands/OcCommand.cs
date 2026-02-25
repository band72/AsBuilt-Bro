using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class OcCommand : ICommand
{
    public string Name => "OC";
    public string Description => "Sets the current occupied station. Usage: OC <PointId>";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        // Executes exactly like STN (Station) command
        return new StnCommand().ExecuteAsync(args, context);
    }
}
