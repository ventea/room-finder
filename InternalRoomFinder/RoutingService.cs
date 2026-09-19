namespace InternalRoomFinder;

internal sealed class RoutingService
{
    public List<string>? FindRoute(CheckpointNode start, CheckpointNode target)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(target);

        Queue<CheckpointNode> queue = new();
        HashSet<CheckpointNode> visited = [];
        Dictionary<CheckpointNode, (CheckpointNode Parent, PathEdge Edge)> parents = [];

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            CheckpointNode current = queue.Dequeue();

            if (ReferenceEquals(current, target))
            {
                return BuildInstructions(start, target, parents);
            }

            foreach (PathEdge edge in current.Connections)
            {
                if (visited.Add(edge.Target))
                {
                    parents[edge.Target] = (current, edge);
                    queue.Enqueue(edge.Target);
                }
            }
        }

        return null;
    }

    private static List<string> BuildInstructions(
        CheckpointNode start,
        CheckpointNode target,
        IReadOnlyDictionary<CheckpointNode, (CheckpointNode Parent, PathEdge Edge)> parents)
    {
        List<string> instructions = [];
        CheckpointNode current = target;

        while (!ReferenceEquals(current, start))
        {
            (CheckpointNode parent, PathEdge edge) = parents[current];
            instructions.Add(edge.Instruction);
            current = parent;
        }

        instructions.Reverse();
        return instructions;
    }
}
