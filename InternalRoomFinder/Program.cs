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
    
    // DYNAMIC MENU: Fetch and display available locations directly from the JSON lookup
    Console.WriteLine("Tillgängliga platser att söka efter:");
    string availablePlaces = string.Join(" | ", store.GetAvailableNames());
    Console.WriteLine(availablePlaces);
    Console.WriteLine(new string('-', 45) + "\n");

    // REFACTOR: Reuse the validation logic for both start and destination
    var (startInput, startNode) = PromptAndValidateLocation("Ange din nuvarande plats (eller scanna QR): ", "Nuvarande plats");
    var (destInput, destinationNode) = PromptAndValidateLocation("Var vill du gå? ", "Destinationen");

    // SERVER-SIDE PROCESSING (Simulating the secure API boundary)
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

// 3. HELPER METHOD: Reusable input and validation loop
(string InputText, CheckpointNode Node) PromptAndValidateLocation(string promptMessage, string errorContext)
{
    while (true)
    {
        Console.Write(promptMessage);
        string input = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine($"[ERROR] {errorContext} får inte vara tom.\n");
            continue;
        }

        string? nodeId = store.ResolveAlias(input);
        CheckpointNode? node = nodeId != null ? store.GetById(nodeId) : null;

        if (node is not null)
        {
            return (input, node); // Return a tuple with both the human name and the resolved node object
        }

        Console.WriteLine("[ERROR] Kunde inte hitta platsen. Kontrollera stavningen och försök igen.\n");
    }
}
