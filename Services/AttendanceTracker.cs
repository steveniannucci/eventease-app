namespace EventEaseApp.Services;

public class AttendanceTracker
{
    private readonly object _lock = new();
    private Dictionary<int, EventAttendance> _eventAttendanceMap = new();
    
    public event Action? OnAttendanceChanged;

    public class EventAttendance
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public List<AttendeeRecord> Attendees { get; set; } = new();
        public int Capacity { get; set; }
        public int WaitlistCount => Attendees.Count(a => a.Status == AttendanceStatus.Waitlisted);
        public int CheckedInCount => Attendees.Count(a => a.Status == AttendanceStatus.CheckedIn);
        public int RegisteredCount => Attendees.Count(a => a.Status == AttendanceStatus.Registered);
        public int AvailableSpots => Math.Max(0, Capacity - Attendees.Count(a => a.Status != AttendanceStatus.Cancelled));
        public double AttendanceRate => RegisteredCount > 0 ? (double)CheckedInCount / RegisteredCount * 100 : 0;
    }

    public class AttendeeRecord
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public DateTime? CheckInTime { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public enum AttendanceStatus
    {
        Registered,
        CheckedIn,
        NoShow,
        Cancelled,
        Waitlisted
    }

    // Event management
    public void CreateEvent(int eventId, string eventName, DateTime eventDate, int capacity = 100)
    {
        if (!_eventAttendanceMap.ContainsKey(eventId))
        {
            _eventAttendanceMap[eventId] = new EventAttendance
            {
                EventId = eventId,
                EventName = eventName,
                EventDate = eventDate,
                Capacity = capacity
            };
            NotifyChanged();
        }
    }

    public EventAttendance? GetEventAttendance(int eventId)
    {
        return _eventAttendanceMap.TryGetValue(eventId, out var attendance) ? attendance : null;
    }

    public IReadOnlyList<EventAttendance> GetAllEvents()
    {
        return _eventAttendanceMap.Values.ToList().AsReadOnly();
    }

    // Attendee registration
    public bool RegisterAttendee(int eventId, string userId, string userName, string email)
    {
        if (!_eventAttendanceMap.TryGetValue(eventId, out var eventAttendance))
        {
            return false;
        }

        if (eventAttendance.Attendees.Any(a => a.UserId == userId && a.Status != AttendanceStatus.Cancelled))
        {
            return false; // Already registered
        }

        var status = eventAttendance.AvailableSpots > 0 
            ? AttendanceStatus.Registered 
            : AttendanceStatus.Waitlisted;

        eventAttendance.Attendees.Add(new AttendeeRecord
        {
            UserId = userId,
            UserName = userName,
            Email = email,
            RegistrationDate = DateTime.UtcNow,
            Status = status
        });

        NotifyChanged();
        return true;
    }

    // Check-in functionality
    public bool CheckInAttendee(int eventId, string userId)
    {
        if (!_eventAttendanceMap.TryGetValue(eventId, out var eventAttendance))
        {
            return false;
        }

        var attendee = eventAttendance.Attendees.FirstOrDefault(a => a.UserId == userId);
        if (attendee == null || attendee.Status == AttendanceStatus.Cancelled)
        {
            return false;
        }

        attendee.Status = AttendanceStatus.CheckedIn;
        attendee.CheckInTime = DateTime.UtcNow;
        NotifyChanged();
        return true;
    }

    // Cancel registration
    public bool CancelRegistration(int eventId, string userId)
    {
        if (!_eventAttendanceMap.TryGetValue(eventId, out var eventAttendance))
        {
            return false;
        }

        var attendee = eventAttendance.Attendees.FirstOrDefault(a => a.UserId == userId);
        if (attendee == null)
        {
            return false;
        }

        attendee.Status = AttendanceStatus.Cancelled;
        NotifyChanged();
        return true;
    }

    // Mark no-show
    public bool MarkNoShow(int eventId, string userId)
    {
        if (!_eventAttendanceMap.TryGetValue(eventId, out var eventAttendance))
        {
            return false;
        }

        var attendee = eventAttendance.Attendees.FirstOrDefault(a => a.UserId == userId);
        if (attendee == null || attendee.Status == AttendanceStatus.CheckedIn)
        {
            return false;
        }

        attendee.Status = AttendanceStatus.NoShow;
        NotifyChanged();
        return true;
    }

    // Get attendee list for event
    public IReadOnlyList<AttendeeRecord> GetAttendees(int eventId, AttendanceStatus? filterByStatus = null)
    {
        if (!_eventAttendanceMap.TryGetValue(eventId, out var eventAttendance))
        {
            return new List<AttendeeRecord>().AsReadOnly();
        }

        var attendees = eventAttendance.Attendees.AsEnumerable();
        if (filterByStatus.HasValue)
        {
            attendees = attendees.Where(a => a.Status == filterByStatus.Value);
        }

        return attendees.ToList().AsReadOnly();
    }

    // Statistics
    public Dictionary<string, object> GetEventStatistics(int eventId)
    {
        var attendance = GetEventAttendance(eventId);
        if (attendance == null)
        {
            return new Dictionary<string, object>();
        }

        return new Dictionary<string, object>
        {
            ["TotalRegistered"] = attendance.RegisteredCount,
            ["CheckedIn"] = attendance.CheckedInCount,
            ["NoShows"] = attendance.Attendees.Count(a => a.Status == AttendanceStatus.NoShow),
            ["Cancelled"] = attendance.Attendees.Count(a => a.Status == AttendanceStatus.Cancelled),
            ["Waitlisted"] = attendance.WaitlistCount,
            ["AttendanceRate"] = $"{attendance.AttendanceRate:F1}%",
            ["AvailableSpots"] = attendance.AvailableSpots
        };
    }

    private void NotifyChanged() => OnAttendanceChanged?.Invoke();
}
