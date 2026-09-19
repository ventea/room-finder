namespace InternalRoomFinder;

// 1. MODELS: We define the building as a graph rather than a map
public class CheckpointNode
{
    public string Id { get; set; }          // The QR code's hidden ID (e.g. "QR_ENTRANCE")
    public string Name { get; set; }        // Descriptive internal name (e.g. "Main entrance")
    public List<PathEdge> Connections { get; set; } = new List<PathEdge>();
}

public class PathEdge
{
    public CheckpointNode Target { get; set; }
    public string Instruction { get; set; } // The physical instructions between two points
}

public class JsonCheckpoint
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<JsonConnection> Connections { get; set; } = [];
}

public class JsonConnection
{
    public string TargetId { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
}