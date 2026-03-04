using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class SyncPtsCommand : ICommand
{
    public string Name => "SYNC-PTS";
    public string Description => "Sync all points into the database. Usage: SYNC-PTS";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (context.SyncPointsAction != null)
        {
            context.Log("Syncing points to database...");
            context.SyncPointsAction.Invoke();
            context.Log("Sync complete.");
        }
        else
        {
            context.Log("Error: Sync action not configured in context.");
        }

        return Task.CompletedTask;
    }
}
