using System.Security.Cryptography;

namespace InternalRoomFinder;

internal sealed class NavigationApi
{
    private readonly TopologyStore _topologyStore;
    private readonly RoutingService _routingService;
    private readonly Dictionary<string, NavigationSession> _sessions = new(StringComparer.Ordinal);
    private readonly Lock _sessionLock = new();
    private const string ConfigPath = "appsettings.json";

    public NavigationApi()
    {
        _topologyStore = new TopologyStore();
        _topologyStore.LoadFromConfig(ConfigPath);
        _routingService = new RoutingService();
    }
    
    public bool IsKnownLocation(string query) => _topologyStore.ContainsAlias(query);

    public NavigationStep? StartNavigation(NavigationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var start = _topologyStore.ResolveAlias(request.CurrentLocationQuery);
        var target = _topologyStore.ResolveAlias(request.TargetDestinationQuery);
        var route = _routingService.FindRoute(start, target);

        if (route is null)
        {
            return null;
        }

        if (route.Count == 0)
        {
            return new NavigationStep(
                string.Empty,
                "You are already at your destination.",
                1,
                false);
        }

        var sessionId = CreateSessionId();
        NavigationSession session = new(route);
        var firstInstruction = session.PendingInstructions.Dequeue();

        lock (_sessionLock)
        {
            _sessions.Add(sessionId, session);
        }

        return new NavigationStep(
            sessionId,
            firstInstruction,
            session.NextStepNumber++,
            session.PendingInstructions.Count > 0);
    }

    public NavigationStep? GetNextInstruction(string sessionId, int stepNumber)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || stepNumber < 1)
        {
            return null;
        }

        lock (_sessionLock)
        {
            if (!_sessions.TryGetValue(sessionId, out NavigationSession? session) ||
                session.PendingInstructions.Count == 0 ||
                session.NextStepNumber != stepNumber)
            {
                return null;
            }

            var instruction = session.PendingInstructions.Dequeue();
            var hasNext = session.PendingInstructions.Count > 0;
            session.NextStepNumber++;

            if (!hasNext)
            {
                _sessions.Remove(sessionId);
            }

            return new NavigationStep(sessionId, instruction, stepNumber, hasNext);
        }
    }

    private string CreateSessionId()
    {
        string sessionId;

        do
        {
            sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        }
        while (HasSession(sessionId));

        return sessionId;
    }

    private bool HasSession(string sessionId)
    {
        lock (_sessionLock)
        {
            return _sessions.ContainsKey(sessionId);
        }
    }

    private sealed class NavigationSession(IReadOnlyCollection<string> instructions)
    {
        public Queue<string> PendingInstructions { get; } = new(instructions);
        public int NextStepNumber { get; set; } = 1;
    }
}
