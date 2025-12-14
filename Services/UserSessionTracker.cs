namespace EventEaseApp.Services;

public class UserSessionTracker
{
    private readonly object _lock = new();
    private string? _userId;
    private string? _userName;
    private List<int> _registeredEventIds = new();
    private Dictionary<string, object> _sessionData = new();

    public event Action? OnStateChanged;

    // User identification
    public string? UserId
    {
        get => _userId;
        set
        {
            if (_userId != value)
            {
                _userId = value;
                NotifyStateChanged();
            }
        }
    }

    public string? UserName
    {
        get => _userName;
        set
        {
            if (_userName != value)
            {
                _userName = value;
                NotifyStateChanged();
            }
        }
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_userId);

    // Event registration tracking
    public IReadOnlyList<int> RegisteredEventIds => _registeredEventIds.AsReadOnly();

    public void RegisterForEvent(int eventId)
    {
        lock (_lock)
        {
            if (!_registeredEventIds.Contains(eventId))
            {
                _registeredEventIds.Add(eventId);
                NotifyStateChanged();
            }
        }
    }

    public void UnregisterFromEvent(int eventId)
    {
        if (_registeredEventIds.Remove(eventId))
        {
            NotifyStateChanged();
        }
    }

    public bool IsRegisteredForEvent(int eventId)
    {
        return _registeredEventIds.Contains(eventId);
    }

    // Generic session data storage
    public void SetSessionData<T>(string key, T value)
    {
        _sessionData[key] = value!;
        NotifyStateChanged();
    }

    public T? GetSessionData<T>(string key)
    {
        if (_sessionData.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public bool TryGetSessionData<T>(string key, out T? value)
    {
        if (_sessionData.TryGetValue(key, out var objValue) && objValue is T typedValue)
        {
            value = typedValue;
            return true;
        }
        value = default;
        return false;
    }

    public void RemoveSessionData(string key)
    {
        if (_sessionData.Remove(key))
        {
            NotifyStateChanged();
        }
    }

    // Session lifecycle
    public void ClearSession()
    {
        _userId = null;
        _userName = null;
        _registeredEventIds.Clear();
        _sessionData.Clear();
        NotifyStateChanged();
    }

    public DateTime SessionStartTime { get; } = DateTime.UtcNow;

    public TimeSpan SessionDuration => DateTime.UtcNow - SessionStartTime;

    // Notify subscribers of state changes
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
