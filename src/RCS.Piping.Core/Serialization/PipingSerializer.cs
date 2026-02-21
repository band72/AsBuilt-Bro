using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RCS.Piping.Core.Models;

namespace RCS.Piping.Core.Serialization;

public class PipingSerializer
{
    private class NetworkState
    {
        public List<PipeRun> Runs { get; set; } = new();
        public List<PipeStructure> Structures { get; set; } = new();
    }

    public static string Serialize(PipeNetwork network)
    {
        var state = new NetworkState
        {
            Runs = network.GetAllRuns().ToList(),
            Structures = network.GetAllStructures().ToList()
        };
        
        return JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
    }

    public static void DeserializeInto(string json, PipeNetwork network)
    {
        var state = JsonSerializer.Deserialize<NetworkState>(json);
        if (state == null) return;

        // Clear existing? Or merge?
        // For "Load", usually we want to clear or replace.
        // But PipeNetwork doesn't have a Clear().
        // We'll implement Clear or just add.
        
        // Let's assume we just add/overwrite by ID.
        foreach(var run in state.Runs)
        {
            network.AddRun(run);
        }
        foreach(var str in state.Structures)
        {
            network.AddStructure(str);
        }
    }
    
    public static void SaveToFile(PipeNetwork network, string filePath)
    {
        var json = Serialize(network);
        File.WriteAllText(filePath, json);
    }
    
    public static void LoadFromFile(PipeNetwork network, string filePath)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);
        var json = File.ReadAllText(filePath);
        DeserializeInto(json, network);
    }
}
