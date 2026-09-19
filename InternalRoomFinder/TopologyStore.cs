using System.Globalization;
using System.Text;
using System.Text.Json;

namespace InternalRoomFinder;

internal sealed class TopologyStore
{
    private Dictionary<string, CheckpointNode> _nodes = [];
    private Dictionary<string, string> _normalizedAliasLookup = [];

    public void LoadFromConfig(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file was not found: {filePath}", filePath);
        }

        string jsonString = File.ReadAllText(filePath);
        ConfigurationRoot config = JsonSerializer.Deserialize<ConfigurationRoot>(jsonString)
            ?? throw new InvalidDataException("Configuration is empty.");

        Dictionary<string, CheckpointNode> nodes = BuildNodes(config);
        Dictionary<string, string> aliases = BuildAliases(config, nodes);
        LinkNodes(config, nodes);

        _nodes = nodes;
        _normalizedAliasLookup = aliases;
    }

    public bool ContainsAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return false;
        }

        return _normalizedAliasLookup.ContainsKey(NormalizeString(alias));
    }

    public CheckpointNode ResolveAlias(string alias)
    {
        string normalizedKey = NormalizeString(alias);

        if (!_normalizedAliasLookup.TryGetValue(normalizedKey, out string? nodeId) ||
            !_nodes.TryGetValue(nodeId, out CheckpointNode? node))
        {
            throw new KeyNotFoundException("The requested location does not exist.");
        }

        return node;
    }

    private static Dictionary<string, CheckpointNode> BuildNodes(ConfigurationRoot config)
    {
        List<JsonCheckpoint> checkpoints = config.NetworkTopology
            ?? throw new InvalidDataException("NetworkTopology is required.");

        Dictionary<string, CheckpointNode> nodes = new(StringComparer.Ordinal);

        foreach (JsonCheckpoint checkpoint in checkpoints)
        {
            if (string.IsNullOrWhiteSpace(checkpoint.Id))
            {
                throw new InvalidDataException("Every checkpoint must have an ID.");
            }

            if (!nodes.TryAdd(checkpoint.Id, new CheckpointNode { Id = checkpoint.Id }))
            {
                throw new InvalidDataException($"Duplicate checkpoint ID: {checkpoint.Id}");
            }
        }

        return nodes;
    }

    private static Dictionary<string, string> BuildAliases(
        ConfigurationRoot config,
        IReadOnlyDictionary<string, CheckpointNode> nodes)
    {
        Dictionary<string, string> sourceAliases = config.SecureAliasLookup
            ?? throw new InvalidDataException("SecureAliasLookup is required.");
        Dictionary<string, string> aliases = new(StringComparer.Ordinal);

        foreach ((string alias, string nodeId) in sourceAliases)
        {
            string normalizedAlias = NormalizeString(alias);

            if (string.IsNullOrEmpty(normalizedAlias))
            {
                throw new InvalidDataException("Aliases must contain searchable text.");
            }

            if (string.IsNullOrWhiteSpace(nodeId) || !nodes.ContainsKey(nodeId))
            {
                throw new InvalidDataException($"Alias '{alias}' references an unknown checkpoint.");
            }

            if (!aliases.TryAdd(normalizedAlias, nodeId))
            {
                throw new InvalidDataException($"Aliases collide after normalization: '{alias}'.");
            }
        }

        return aliases;
    }

    private static void LinkNodes(
        ConfigurationRoot config,
        IReadOnlyDictionary<string, CheckpointNode> nodes)
    {
        Dictionary<string, string> instructions = config.SecureInstructions
            ?? throw new InvalidDataException("SecureInstructions is required.");

        foreach (JsonCheckpoint checkpoint in config.NetworkTopology!)
        {
            CheckpointNode source = nodes[checkpoint.Id!];

            foreach (JsonConnection connection in checkpoint.Connections ?? [])
            {
                if (string.IsNullOrWhiteSpace(connection.TargetId) ||
                    !nodes.TryGetValue(connection.TargetId, out CheckpointNode? target))
                {
                    throw new InvalidDataException(
                        $"Checkpoint '{checkpoint.Id}' contains an unknown connection target.");
                }

                if (string.IsNullOrWhiteSpace(connection.InstructionId) ||
                    !instructions.TryGetValue(connection.InstructionId, out string? instruction) ||
                    string.IsNullOrWhiteSpace(instruction))
                {
                    throw new InvalidDataException(
                        $"Connection from '{checkpoint.Id}' has a missing instruction.");
                }

                source.Connections.Add(new PathEdge
                {
                    Target = target,
                    Instruction = instruction
                });
            }
        }
    }

    private static string NormalizeString(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string formD = text.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(formD.Length);

        foreach (char character in formD)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category is not UnicodeCategory.NonSpacingMark
                and not UnicodeCategory.SpacingCombiningMark
                and not UnicodeCategory.EnclosingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
