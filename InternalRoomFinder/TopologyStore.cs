using System.Globalization;
using System.Text;
using System.Text.Json;

namespace InternalRoomFinder;

public class TopologyStore
{
    private readonly Dictionary<string, CheckpointNode> _nodes = [];
    
    // Secure secondary dictionary for normalized searches (lowercase, no accents)
    private readonly Dictionary<string, string> _normalizedAliasLookup = new();

    public CheckpointNode? GetById(string id) => _nodes.GetValueOrDefault(id);
    
    // SECURE ALIAS RESOLUTION: Fully case and diacritic insensitive
    public string? ResolveAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        
        string normalizedKey = NormalizeString(alias);
        return _normalizedAliasLookup.GetValueOrDefault(normalizedKey);
    }

    public void LoadFromConfig(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Hittade inte konfigurationsfilen: {filePath}");
        }

        string jsonString = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<ConfigurationRoot>(jsonString);

        if (config == null) return;
        
        // Build the normalized lookup index in memory at startup strictly on the server
        _normalizedAliasLookup.Clear();
        foreach (var kvp in config.SecureAliasLookup)
        {
            string normalizedKey = NormalizeString(kvp.Key);
            _normalizedAliasLookup[normalizedKey] = kvp.Value;
        }

        // Step 1: Create all anonymized nodes
        foreach (var jsonCp in config.NetworkTopology)
        {
            _nodes[jsonCp.Id] = new CheckpointNode { Id = jsonCp.Id };
        }

        // Step 2: Link nodes together and inject secure instructions
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

    private static string NormalizeString(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string formD = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new();

        foreach (char ch in formD)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);

            if (category != UnicodeCategory.NonSpacingMark &&
                category != UnicodeCategory.SpacingCombiningMark &&
                category != UnicodeCategory.EnclosingMark)
            {
                sb.Append(ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
