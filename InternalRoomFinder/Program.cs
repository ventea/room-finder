using System.Text.Json;
using InternalRoomFinder;

var navigationApi = NavigationApi.CreateFromConfig("appsettings.json");

while (true)
{
    Console.Clear();
    Console.WriteLine("=============================================");
    Console.WriteLine("    INTERNAL ROOM-FINDER: ZERO-KNOWLEDGE MVP ");
    Console.WriteLine("=============================================");
    Console.WriteLine("[SECURITY] User authenticated via Corporate SSO.");
    Console.WriteLine("[SECURITY] Location resolution remains server-side.");
    Console.WriteLine(new string('-', 45) + "\n");

    string currentLocation = PromptAndValidateLocation(
        "Enter your current location (or scan QR): ",
        "Current location");
    string destination = PromptAndValidateLocation(
        "Where do you want to go? ",
        "Destination");

    NavigationRequest request = new(currentLocation, destination);
    Console.Clear();
    Console.WriteLine("=============================================");
    Console.WriteLine("      [API NETWORK TRAFFIC: OUTBOUND]        ");
    Console.WriteLine("=============================================");
    Console.WriteLine("  POST /api/navigation/v1/routing HTTP/1.1");
    Console.WriteLine("  Content-Type: application/json\n");
    Console.WriteLine("  JSON Payload Sent from Mobile Client:");
    Console.WriteLine(JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(new string('-', 45));
    Console.WriteLine("Press any key to send request to server...");
    Console.ReadKey();

    NavigationStep? step = navigationApi.StartNavigation(request);

    Console.Clear();
    Console.WriteLine("=============================================");
    Console.WriteLine("      [API NETWORK TRAFFIC: INBOUND]         ");
    Console.WriteLine("=============================================");
    Console.WriteLine("  HTTP/1.1 200 OK");
    Console.WriteLine("  Content-Type: application/json\n");
    Console.WriteLine("  JSON Payload Received by Mobile Client:");

    if (step is null)
    {
        Console.WriteLine("  { \"status\": \"Error\", \"code\": \"NO_SAFE_ROUTE_FOUND\" }");
        Console.WriteLine(new string('-', 45));
    }
    else
    {
        Console.WriteLine(JsonSerializer.Serialize(step, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(new string('-', 45));
        Console.WriteLine("\n[ARCHITECTURE NOTE] Only the current instruction crosses the API boundary.");
        Console.WriteLine("The server retains the remaining route behind the session token.");
        Console.WriteLine("\nPress any key to start the step-by-step guidance wizard...");
        Console.ReadKey();

        ShowWizard(navigationApi, step, destination);
    }

    Console.WriteLine("\nPress 'Q' to quit, or any other key for a new search...");
    if (Console.ReadKey().Key == ConsoleKey.Q)
    {
        break;
    }
}

string PromptAndValidateLocation(string promptMessage, string errorContext)
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

        if (input.Length > 200)
        {
            Console.WriteLine($"[ERROR] {errorContext} is too long.\n");
            continue;
        }

        if (navigationApi.IsKnownLocation(input))
        {
            return input;
        }

        Console.WriteLine("[ERROR] Location not found. Please check your spelling and try again.\n");
    }
}

void ShowWizard(NavigationApi api, NavigationStep firstStep, string destination)
{
    NavigationStep? step = firstStep;

    while (step is not null)
    {
        Console.Clear();
        Console.WriteLine("=============================================");
        Console.WriteLine($"  WIZARD: Route to {destination}");
        Console.WriteLine("=============================================");
        Console.WriteLine($"Progress: Step {step.StepNumber}\n");
        Console.WriteLine($">> INSTRUCTION: {step.Instruction}");

        if (!step.HasNext)
        {
            Console.WriteLine($"\n[ARRIVED] You have reached your destination! Welcome to {destination}.");
            return;
        }

        Console.WriteLine("\n[Press any key when you are ready for the next step...]");
        Console.ReadKey();
        step = api.GetNextInstruction(step.SessionId, step.StepNumber + 1);
    }

    Console.WriteLine("[ERROR] The navigation session expired.");
}