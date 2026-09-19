using System.Globalization;
using System.Text;
using System.Text.Json;

namespace InternalRoomFinder;

public class TopologyStore
{
    private readonly Dictionary<string, CheckpointNode> _nodes = [];
    
    // Vi lagrar nycklarna i sin råa form för menyn, men mappar dem till nod-id:n
    private Dictionary<string, string> _aliasLookup = [];
    
    // En sekundär dictionary där nyckeln är helt normaliserad (utan skiftläge/accenter)
    private readonly Dictionary<string, string> _normalizedAliasLookup = new();

    public CheckpointNode? GetById(string id) => _nodes.GetValueOrDefault(id);
    
    // Säkert uppslag: Nu med normaliserad sökning
    public string? ResolveAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias)) return null;
        
        string normalizedKey = NormalizeString(alias);
        return _normalizedAliasLookup.GetValueOrDefault(normalizedKey);
    }

    // Hämtar alla tillgängliga rumsnamn (originalform) sorterade i bokstavsordning för menyn
    public IEnumerable<string> GetAvailableNames() => _aliasLookup.Keys.OrderBy(k => k);

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
        
        // Bygg upp den normaliserade sökdatabasen
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

    private static string NormalizeString(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        // 1. FormDecompose delar upp tecken som 'é' i basbokstaven 'e' + accenttecknet '´'
        string formD = text.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new();

        foreach (char ch in formD)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(ch);

            // Vi filtrerar bort alla typer av modifieringstecken/accenter (Marks)
            if (category != UnicodeCategory.NonSpacingMark &&
                category != UnicodeCategory.SpacingCombiningMark &&
                category != UnicodeCategory.EnclosingMark)
            {
                sb.Append(ch);
            }
        }

        // 2. Gör om till gemener och återställ till standard Unicode-form (FormC)
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
