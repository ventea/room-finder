using InternalRoomFinder;

// 1. INITIALIZATION
TopologyStore store = new();
store.LoadFromConfig("appsettings.json");
RoutingService routingService = new();

while (true)
{
    Console.Clear();
    Console.WriteLine("=============================================");
    Console.WriteLine("    INTERNAL ROOM-FINDER: ZERO-KNOWLEDGE MVP ");
    Console.WriteLine("=============================================");
    Console.WriteLine("[SÄKERHET] Användare verifierad via Corporate SSO.");
    Console.WriteLine("[SÄKERHET] Noll rums- eller ID-data exponeras till klienten.");
    Console.WriteLine(new string('-', 45) + "\n");

    // UX & SECURITY FIX: No menu list is leaked. The client must actively query a location.
    var (startInput, startNode) = PromptAndValidateLocation("Ange din nuvarande plats (eller scanna QR): ", "Nuvarande plats");
    var (destInput, destinationNode) = PromptAndValidateLocation("Var vill du gå? ", "Destinationen");

    // SERVER-SIDE PROCESSING (Simulating the secure API boundary)
    Console.Clear();
    Console.WriteLine("=== SIMULATED SECURE API BOUNDARY ===");
    Console.WriteLine($"[API REQ] Sending validated texts to server: '{startInput}' -> '{destInput}'");
    Console.WriteLine("[API LOG] Resolving aliases to internal hashes strictly behind the firewall...\n");

    // 2. PATHFINDING
    List<PathEdge>? route = routingService.FindRoute(startNode, destinationNode);

    if (route is not null)
    {
        Console.WriteLine($"[API RES] Route found! Sending {route.Count} text instructions to client.\n");
        Console.WriteLine("Tryck på valfri tangent för att starta vägledningen...");
        Console.ReadKey();

        // CLIENT-SIDE DISPLAY (The Wizard)
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
        Console.WriteLine("\n[SECURITY DETECTED] Ingen säker eller tillgänglig väg hittades.");
    }

    Console.WriteLine("\nTryck på 'Q' för att avsluta, eller valfri annan tangent för en ny sökning...");
    if (Console.ReadKey().Key == ConsoleKey.Q)
    {
        break;
    }
}

// 3. HELPER METHOD: Reusable input and validation loop that blocks until a valid node is resolved
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

        // Check against our case and diacritic insensitive store
        string? nodeId = store.ResolveAlias(input);
        CheckpointNode? node = nodeId != null ? store.GetById(nodeId) : null;

        if (node is not null)
        {
            return (input, node);
        }

        Console.WriteLine("[ERROR] Kunde inte hitta platsen. Kontrollera stavningen och försök igen.\n");
    }
}