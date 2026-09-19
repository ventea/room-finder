namespace InternalRoomFinder;

public class CheckpointNode
{
    public string Id { get; set; } = string.Empty;
    public List<PathEdge> Connections { get; set; } = [];
}

public class PathEdge
{
    public CheckpointNode Target { get; set; } = null!;
    public string Instruction { get; set; } = string.Empty;
}

public class ConfigurationRoot
{
    public List<JsonCheckpoint> NetworkTopology { get; set; } = [];
    public Dictionary<string, string> SecureInstructions { get; set; } = [];
    public Dictionary<string, string> SecureAliasLookup { get; set; } = [];
}

public class JsonCheckpoint
{
    public string Id { get; set; } = string.Empty;
    public List<JsonConnection> Connections { get; set; } = [];
}

public class JsonConnection
{
    public string TargetId { get; set; } = string.Empty;
    public string InstructionId { get; set; } = string.Empty;
}