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
    Console.WriteLine("[SECURITY] User authenticated via Corporate SSO.");
    Console.WriteLine("[SECURITY] Zero room or ID data exposed to the client.");
    Console.WriteLine(new string('-', 45) + "\n");

    var (startInput, startNode) = PromptAndValidateLocation("Enter your current location (or scan QR): ", "Current location");
    var (destInput, destinationNode) = PromptAndValidateLocation("Where do you want to go? ", "Destination");

    // VISUALIZATION: Secure API Request Payload
    Console.Clear();
    Console.WriteLine("=============================================");
    Console.WriteLine("      [API NETWORK TRAFFIC: OUTBOUND]        ");
    Console.WriteLine("=============================================");
    Console.WriteLine("  POST /api/navigation/v1/routing HTTP/1.1");
    Console.WriteLine("  Authorization: Bearer <SSO_TOKEN_HIDDEN>");
    Console.WriteLine("  Content-Type: application/json\n");
    Console.WriteLine("  JSON Payload Sent from Mobile Client:");
    Console.WriteLine("  {");
    Console.WriteLine($"    \"currentLocationQuery\": \"{startInput}\",");
    Console.WriteLine($"    \"targetDestinationQuery\": \"{destInput}\"");
    Console.WriteLine("  }");
    Console.WriteLine(new string('-', 45));
    Console.WriteLine("Press any key to send request to server...");
    Console.ReadKey();

    // SECURE: Server executes algorithm and returns ONLY a list of strings
    List<string>? route = routingService.FindRoute(startNode, destinationNode);

    // VISUALIZATION: Secure API Response Payload
    Console.Clear();
    Console.WriteLine("=============================================");
    Console.WriteLine("      [API NETWORK TRAFFIC: INBOUND]         ");
    Console.WriteLine("=============================================");
    Console.WriteLine("  HTTP/1.1 200 OK");
    Console.WriteLine("  Content-Type: application/json\n");
    Console.WriteLine("  JSON Payload Received by Mobile Client:");
    
    if (route is not null)
    {
        Console.WriteLine("  {");
        Console.WriteLine($"    \"status\": \"Success\",");
        Console.WriteLine($"    \"totalSteps\": {route.Count},");
        Console.WriteLine("    \"instructions\": [");
        
        for (int i = 0; i < route.Count; i++)
        {
            string comma = (i == route.Count - 1) ? "" : ",";
            Console.WriteLine($"      {{ \"step\": {i + 1}, \"text\": \"{route[i]}\" }}{comma}");
        }
        
        Console.WriteLine("    ]");
        Console.WriteLine("  }");
        Console.WriteLine(new string('-', 45));
        Console.WriteLine("\n[ARCHITECTURE NOTE] Notice that the client receives the plaintext instructions,");
        Console.WriteLine("but no internal database hashes or structural relations ever leave the server.");
        Console.WriteLine("\nPress any key to start the step-by-step guidance wizard...");
        Console.ReadKey();

        // 2. CLIENT-SIDE DISPLAY (The Wizard)
        int currentStep = 1;
        int totalSteps = route.Count;

        foreach (var instruction in route)
        {
            Console.Clear();
            Console.WriteLine("=============================================");
            Console.WriteLine($"  WIZARD: Route to {destInput}  ");
            Console.WriteLine("=============================================");
            Console.WriteLine($"Progress: [Step {currentStep} of {totalSteps}]\n");
            
            // Display exactly ONE instruction at a time to prevent layout extraction
            Console.WriteLine($">> INSTRUCTION: {instruction}");
            
            currentStep++;

            if (currentStep <= totalSteps)
            {
                Console.WriteLine("\n[Press any key when you are ready for the next step...]");
                Console.ReadKey();
            }
        }
        
        Console.WriteLine($"\n[ARRIVED] You have reached your destination! Welcome to {destInput}.");
    }
    else
    {
        Console.WriteLine("  {");
        Console.WriteLine("    \"status\": \"Error\",");
        Console.WriteLine("    \"code\": \"NO_SAFE_ROUTE_FOUND\"");
        Console.WriteLine("  }");
        Console.WriteLine(new string('-', 45));
    }

    Console.WriteLine("\nPress 'Q' to quit, or any other key for a new search...");
    if (Console.ReadKey().Key == ConsoleKey.Q)
    {
        break;
    }
}

// 3. HELPER METHOD
(string InputText, CheckpointNode Node) PromptAndValidateLocation(string promptMessage, string errorContext)
{
    while (true)
    {
        Console.Write(promptMessage);
        string input = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            Console.WriteLine($"[ERROR] {errorContext} cannot be empty.\n");
            continue;
        }

        string? nodeId = store.ResolveAlias(input);
        CheckpointNode? node = nodeId != null ? store.GetById(nodeId) : null;

        if (node is not null)
        {
            return (input, node);
        }

        Console.WriteLine("[ERROR] Location not found. Please check your spelling and try again.\n");
    }
}
