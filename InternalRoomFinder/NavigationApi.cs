using System.Security.Cryptography;

namespace InternalRoomFinder;

internal sealed class NavigationApi
{
    private readonly TopologyStore _topologyStore;
    private readonly RoutingService _routingService;
    private readonly Dictionary<string, NavigationSession> _sessions = new(StringComparer.Ordinal);
    private readonly object _sessionLock = new();

    public NavigationApi(TopologyStore topologyStore, RoutingService routingService)
    {
        _topologyStore = topologyStore;
        _routingService = routingService;
    }

    public static NavigationApi CreateFromConfig(string filePath)
    {
        TopologyStore topologyStore = new();
        topologyStore.LoadFromConfig(filePath);
        return new NavigationApi(topologyStore, new RoutingService());
    }

    public bool IsKnownLocation(string query) => _topologyStore.ContainsAlias(query);

    public NavigationStep? StartNavigation(NavigationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        CheckpointNode start = _topologyStore.ResolveAlias(request.CurrentLocationQuery);
        CheckpointNode target = _topologyStore.ResolveAlias(request.TargetDestinationQuery);
        List<string>? route = _routingService.FindRoute(start, target);

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

        string sessionId = CreateSessionId();
        NavigationSession session = new(route);
        string firstInstruction = session.PendingInstructions.Dequeue();

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

            string instruction = session.PendingInstructions.Dequeue();
            bool hasNext = session.PendingInstructions.Count > 0;
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
