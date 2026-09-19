using InternalRoomFinder;

// 1. INITIALIZATION: Load topology and instantiate core services
TopologyStore store = new();
store.LoadFromConfig("appsettings.json");

RoutingService routingService = new();

// Retrieve the start and destination nodes from the store
CheckpointNode? startNode = store.GetById("QR_ENTRANCE");
CheckpointNode? destinationNode = store.GetById("QR_GRIPEN");

if (startNode is null || destinationNode is null)
{
    Console.WriteLine("[ERROR] Kunde inte ladda start- eller målnod från konfigurationen.");
    return;
}

// 2. USER FLOW SIMULATION
string scannedQrCode = startNode.Id; 

Console.WriteLine("=== INTERNAL ROOM-FINDER MVP ===");
Console.WriteLine("[SECURITY CHECK] User Authenticated via Corporate SSO.");
Console.WriteLine($"[LOCATION] Scanned QR Code: {scannedQrCode} ({startNode.Name})");
Console.WriteLine($"[DESTINATION] Searching route to: {destinationNode.Name}\n");

// 3. ROUTING: Delegate pathfinding logic to the dedicated RoutingService
List<PathEdge>? route = routingService.FindRoute(startNode, destinationNode);

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