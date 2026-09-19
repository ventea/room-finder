using RoomFinder;

// 1. INITIALIZATION: Build the building's network in memory (topology)
// Note: No X/Y coordinates, floor plans, or geometric data are stored!
CheckpointNode entrance = new() { Id = "QR_ENTRANCE", Name = "Huvudentrén" };
CheckpointNode coffeeStation = new() { Id = "QR_COFFEE_V1", Name = "Kaffestationen Våning 1" };
CheckpointNode elevatorV1 = new() { Id = "QR_ELEV_V1", Name = "Hisshallen Våning 1" };
CheckpointNode elevatorV2 = new() { Id = "QR_ELEV_V2", Name = "Hisshallen Våning 2" };
CheckpointNode confJupiter = new() { Id = "QR_CONF_GRIPEN", Name = "Konferensrum Gripen" };

// Create connections (bidirectional paths with instructions)
entrance.Connections.Add(new() { Target = coffeeStation, Instruction = "Gå rakt fram i 15 meter, ta sedan höger vid den svarta soffan." });
coffeeStation.Connections.Add(new() { Target = entrance, Instruction = "Gå mot utgången, passera förbi soffgrupperna på vänster sida." });

coffeeStation.Connections.Add(new() { Target = elevatorV1, Instruction = "Fortsätt förbi kaffemaskinen, ta dörren till vänster in i hisshallen." });
elevatorV1.Connections.Add(new() { Target = coffeeStation, Instruction = "Gå ut genom dörren och ta direkt höger mot kaffestationen." });

// Vertical connection (stairs/elevators are handled seamlessly in the graph)
elevatorV1.Connections.Add(new() { Target = elevatorV2, Instruction = "Ta hissen eller trapporna upp till Våning 2." });
elevatorV2.Connections.Add(new() { Target = elevatorV1, Instruction = "Ta hissen eller trapporna ner till Våning 1." });

elevatorV2.Connections.Add(new() { Target = confJupiter, Instruction = "Gå ut ur hisshallen, ta höger i korridoren. Rummet är andra dörren på vänster sida." });
confJupiter.Connections.Add(new() { Target = elevatorV2, Instruction = "Gå ut ur rummet, ta höger och följ korridoren fram till hisshallen." });


// 2. USER FLOW SIMULATION
// The user scanned the QR code at the entrance and is searching for "Konferensrum Jupiter"
string scannedQrCode = "QR_ENTRANCE"; 
CheckpointNode startNode = entrance; // The system maps the QR code's ID to the starting node
CheckpointNode destinationNode = confJupiter;

Console.WriteLine("=== INTERNAL ROOM-FINDER MVP ===");
Console.WriteLine("[SECURITY CHECK] User Authenticated via Corporate SSO.");
Console.WriteLine($"[LOCATION] Scanned QR Code: {scannedQrCode} ({startNode.Name})");
Console.WriteLine($"[DESTINATION] Searching route to: {destinationNode.Name}\n");


// 3. SEARCH: Find the shortest path in the network (breadth-first search)
List<PathEdge>? route = FindRoute(startNode, destinationNode);

if (route is not null)
{
    Console.WriteLine("--- GUIDANCE INSTRUCTIONS ---");
    int step = 1;
    foreach (var edge in route)
    {
        Console.WriteLine($"{step}. {edge.Instruction}");
        step++;
    }
    Console.WriteLine("\n[ARRIVED] Du har nått ditt mål!");
}
else
{
    Console.WriteLine("Kunde inte hitta någon säker väg till destinationen.");
}


// 4. SEARCH ALGORITHM (BFS) for navigating the graph without map data
static List<PathEdge>? FindRoute(CheckpointNode start, CheckpointNode target)
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

        if (current == target)
        {
            found = true;
            break;
        }

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
        route.Insert(0, edge); // Insert at the beginning so the list is in forward order
        curr = parentNode[curr];
    }

    return route;
}
