namespace InternalRoomFinder;

internal sealed class CheckpointNode
{
    public required string Id { get; init; }
    public List<PathEdge> Connections { get; } = [];
}

internal sealed class PathEdge
{
    public required CheckpointNode Target { get; init; }
    public required string Instruction { get; init; }
}

internal sealed class ConfigurationRoot
{
    public List<JsonCheckpoint>? NetworkTopology { get; init; }
    public Dictionary<string, string>? SecureInstructions { get; init; }
    public Dictionary<string, string>? SecureAliasLookup { get; init; }
}

internal sealed class JsonCheckpoint
{
    public string? Id { get; init; }
    public List<JsonConnection>? Connections { get; init; }
}

internal sealed class JsonConnection
{
    public string? TargetId { get; init; }
    public string? InstructionId { get; init; }
}

public sealed record NavigationRequest(
    string CurrentLocationQuery,
    string TargetDestinationQuery);

public sealed record NavigationStep(
    string SessionId,
    string Instruction,
    int StepNumber,
    bool HasNext);
