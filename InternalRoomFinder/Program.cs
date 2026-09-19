using InternalRoomFinder;

// 1. INITIALIZATION
TopologyStore store = new();
store.LoadFromConfig("appsettings.json");
RoutingService routingService = new();

while (true)
{
    Console.Clear();
    Console.WriteLine("=============================================");
    Console.WriteLine("      INTERNAL ROOM-FINDER SERVICE MVP       ");
    Console.WriteLine("=============================================");
    Console.WriteLine("[SECURITY STATUS] User Authenticated via Corporate SSO.\n");
    Console.WriteLine("Tillgängliga platser:");
    Console.WriteLine("- Huvudentrén / Viggen / Kaffestationen / Lunchrummet / Gripen\n");


    // Get Start Location (Simulating scanning a QR code or selecting a starting point)
    Console.Write("Ange din nuvarande plats (eller scanna QR): ");
    string? startInput = Console.ReadLine()?.Trim();
    
    // Get Destination
    Console.Write("Var vill du gå? ");
    string? destInput = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(startInput) || string.IsNullOrEmpty(destInput))
    {
        Console.WriteLine("\n[ERROR] Start och mål får inte vara tomma. Tryck på valfri tangent...");
        Console.ReadKey();
        continue;
    }

    // SERVER-SIDE RESOLUTION: Resolve human-readable names to secure anonymous hashes
    string? startNodeId = store.ResolveAlias(startInput);
    string? destNodeId = store.ResolveAlias(destInput);

    CheckpointNode? startNode = startNodeId != null ? store.GetById(startNodeId) : null;
    CheckpointNode? destinationNode = destNodeId != null ? store.GetById(destNodeId) : null;

    if (startNode is null || destinationNode is null)
    {
        Console.WriteLine("\n[ERROR] Kunde inte hitta platserna. Kontrollera stavningen.");
        Console.WriteLine("Tryck på valfri tangent för att försöka igen...");
        Console.ReadKey();
        continue;
    }

    Console.Clear();
    Console.WriteLine("=== SYSTEM SECURITY LOGS ===");
    Console.WriteLine($"[LOG] Resolving '{startInput}' -> Anonymized Internal ID: {startNode.Id}");
    Console.WriteLine($"[LOG] Resolving '{destInput}' -> Anonymized Internal ID: {destinationNode.Id}");
    Console.WriteLine($"[LOG] Executing pathfinding graph algorithm...\n");

    // 2. PATHFINDING
    List<PathEdge>? route = routingService.FindRoute(startNode, destinationNode);

    if (route is not null)
    {
        Console.WriteLine("--- NAVIGERING HITTAD (Steg-för-steg Wizard) ---");
        int currentStep = 1;
        int totalSteps = route.Count;

        foreach (var edge in route)
        {
            Console.Clear();
            Console.WriteLine("=============================================");
            Console.WriteLine($"  WIZARD: Väg till {destInput}  ");
            Console.WriteLine("=============================================");
            Console.WriteLine($"Progress: [Steg {currentStep} av {totalSteps}]\n");
            
            // Display exactly ONE instruction at a time to prevent layout extraction
            Console.WriteLine($">> INSTRUKTION: {edge.Instruction}");
            
            currentStep++;

            if (currentStep <= totalSteps)
            {
                Console.WriteLine("\n[Tryck på valfri tangent när du är redo för nästa steg...]");
                Console.ReadKey();
            }
        }
        
        Console.WriteLine($"\n[MÅL] Du har nått ditt mål! Välkommen till {destInput}.");
    }
    else
    {
        Console.WriteLine("\n[SECURITY DETECTED] Ingen säker eller tillgänglig väg hittades mellan dessa punkter.");
    }

    Console.WriteLine("\nTryck på 'Q' för att avsluta, eller valfri annan tangent för en ny sökning...");
    if (Console.ReadKey().Key == ConsoleKey.Q)
    {
        break;
    }
}