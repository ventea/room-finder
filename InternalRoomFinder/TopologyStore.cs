using System.Text.Json;

namespace InternalRoomFinder;

public class TopologyStore
{
    private readonly Dictionary<string, CheckpointNode> _nodes = [];

    public CheckpointNode? GetById(string id) => _nodes.GetValueOrDefault(id);

    public void LoadFromConfig(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Hittade inte konfigurationsfilen: {filePath}");
        }

        string jsonString = File.ReadAllText(filePath);
        var jsonCheckpoints = JsonSerializer.Deserialize<List<JsonCheckpoint>>(jsonString);

        if (jsonCheckpoints == null) return;

        // Step 1: Create all nodes first (so they exist in memory)
        foreach (var jsonCp in jsonCheckpoints)
        {
            _nodes[jsonCp.Id] = new CheckpointNode 
            { 
                Id = jsonCp.Id, 
                Name = jsonCp.Name 
            };
        }

        // Step 2: Connect the nodes (create edges based on TargetId)
        foreach (var jsonCp in jsonCheckpoints)
        {
            var sourceNode = _nodes[jsonCp.Id];

            foreach (var jsonConn in jsonCp.Connections)
            {
                if (_nodes.TryGetValue(jsonConn.TargetId, out var targetNode))
                {
                    sourceNode.Connections.Add(new PathEdge
                    {
                        Target = targetNode,
                        Instruction = jsonConn.Instruction
                    });
                }
            }
        }
    }
}
