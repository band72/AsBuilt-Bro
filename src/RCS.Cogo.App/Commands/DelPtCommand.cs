using System.Threading.Tasks;
using RCS.Cogo.App.Scripting;

namespace RCS.Cogo.App.Commands;

public class DelPtCommand : ICommand
{
    public string Name => "DELPT";
    public string Description => "DELPT [PointID | StartPt-EndPt] - Deletes point(s) from memory.";

    public Task ExecuteAsync(string[] args, ICogoContext context)
    {
        if (args.Length < 2)
        {
            context.Log("Error: Missing PointID for DELPT command.");
            return Task.CompletedTask;
        }

        string input = args[1];

        if (input.Contains('-'))
        {
            var parts = input.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
            {
                if (start > end) { int temp = start; start = end; end = temp; }
                int count = 0;
                for (int i = start; i <= end; i++)
                {
                    if (context.DeletePoint(i.ToString()))
                    {
                        count++;
                    }
                }
                context.Log($"Deleted {count} point(s) in range {start}-{end}.");
            }
            else
            {
                context.Log($"Error: Invalid range format '{input}'. Expected StartID-EndID.");
            }
        }
        else
        {
            if (context.DeletePoint(input))
            {
                context.Log($"Deleted Point: {input}");
            }
            else
            {
                context.Log($"Error: Point {input} not found.");
            }
        }
        
        return Task.CompletedTask;
    }
}
