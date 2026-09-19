namespace InternalRoomFinder;

public class RoutingService
{
    /// <summary>
    /// Finds the shortest path between two nodes using Breadth-First Search (BFS).
    /// Returns null if no route can be found.
    /// </summary>
    public List<PathEdge>? FindRoute(CheckpointNode start, CheckpointNode target)
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

        // Reconstruct the path backwards from the destination to the start
        List<PathEdge> route = [];
        var curr = target;
        while (curr != start)
        {
            var edge = parentEdge[curr];
            route.Insert(0, edge); // Insert at index 0 to reverse the order into a forward path
            curr = parentNode[curr];
        }
        return route;
    }
}