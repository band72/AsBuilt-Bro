using System.Collections.Concurrent;

namespace RCS.Piping.Core.Models;

public class PipeNetwork
{
    // Thread-safe collections
    public ConcurrentDictionary<string, PipeRun> Runs { get; } = new();
    public ConcurrentDictionary<string, PipeStructure> Structures { get; } = new();

    public void AddRun(PipeRun run)
    {
        Runs.TryAdd(run.Id, run);
    }

    public void AddStructure(PipeStructure structure)
    {
        Structures.TryAdd(structure.Id, structure);
    }

    public bool RemoveRun(string id) => Runs.TryRemove(id, out _);
    public bool RemoveStructure(string id) => Structures.TryRemove(id, out _);

    public IEnumerable<PipeRun> GetAllRuns() => Runs.Values;
    public IEnumerable<PipeStructure> GetAllStructures() => Structures.Values;

    public void Clear()
    {
        Runs.Clear();
        Structures.Clear();
    }
}
