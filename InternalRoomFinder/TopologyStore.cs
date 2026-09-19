using System.Text.Json;

namespace InternalRoomFinder;

public class TopologyStore
{
    private readonly Dictionary<string, CheckpointNode> _nodes = [];
    private Dictionary<string, string> _aliasLookup = [];

    public CheckpointNode? GetById(string id) => _nodes.GetValueOrDefault(id);
    
    // Säkert uppslag: Användaren söker på "Gripen", servern returnerar "nd_1c2b"
    public string? ResolveAlias(string alias) => _aliasLookup.GetValueOrDefault(alias);

    public void LoadFromConfig(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Hittade inte konfigurationsfilen: {filePath}");
        }

        string jsonString = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<ConfigurationRoot>(jsonString);

        if (config == null) return;

        _aliasLookup = config.SecureAliasLookup;

        // Step 1: Create all anonymized nodes
        foreach (var jsonCp in config.NetworkTopology)
        {
            _nodes[jsonCp.Id] = new CheckpointNode { Id = jsonCp.Id };
        }

        // Step 2: Link nodes and inject instructions
        foreach (var jsonCp in config.NetworkTopology)
        {
            var sourceNode = _nodes[jsonCp.Id];

            foreach (var jsonConn in jsonCp.Connections)
            {
                if (_nodes.TryGetValue(jsonConn.TargetId, out var targetNode))
                {
                    string secureInstruction = config.SecureInstructions.GetValueOrDefault(
                        jsonConn.InstructionId, 
                        "[KRYPTERAT]"
                    );

                    sourceNode.Connections.Add(new PathEdge
                    {
                        Target = targetNode,
                        Instruction = secureInstruction
                    });
                }
            }
        }
    }
    
    // Hämtar alla tillgängliga rumsnamn sorterade i bokstavsordning för menyn
    public IEnumerable<string> GetAvailableNames() => _aliasLookup.Keys.OrderBy(k => k);
}