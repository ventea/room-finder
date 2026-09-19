namespace InternalRoomFinder;

public class RoutingService
{
    /// <summary>
    /// Finds the shortest path between two nodes using Breadth-First Search (BFS).
    /// Returns a secure list of plaintext instructions only, exposing no node or graph structural data.
    /// </summary>
    public List<string>? FindRoute(CheckpointNode start, CheckpointNode target)
    {
        Queue<CheckpointNode> queue = new();
        HashSet<CheckpointNode> visited = [];
        Dictionary<CheckpointNode, PathEdge> parentEdge = [];
        Dictionary<CheckpointNode, CheckpointNode> parentNode = [];

        queue.Enqueue(start);
        visited.Add(start);
        bool found = false;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target) { found = true; break; }

            foreach (var edge in current.Connections)
            {
                if (!visited.Contains(edge.Target))
                {
                    visited.Add(edge.Target);
                    parentEdge[edge.Target] = edge;
                    parentNode[edge.Target] = current;
                    queue.Enqueue(edge.Target);
                }
            }
        }

        if (!found) return null;

        // Reconstruct the path backwards, extracting ONLY the plaintext instruction strings
        List<string> instructions = [];
        var curr = target;
        while (curr != start)
        {
            var edge = parentEdge[curr];
            instructions.Insert(0, edge.Instruction); // Securely isolate the text from the node object
            curr = parentNode[curr];
        }
        return instructions;
    }
}