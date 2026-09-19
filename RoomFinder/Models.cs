namespace RoomFinder;

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