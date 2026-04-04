using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Entities;

public class Shift
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Employee Employee { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public ShiftType Type { get; private set; }
    public ShiftStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<ShiftExchangeRequest> _sentExchangeRequests = new();
    public IReadOnlyCollection<ShiftExchangeRequest> SentExchangeRequests => _sentExchangeRequests.AsReadOnly();

    private readonly List<ShiftExchangeRequest> _receivedExchangeRequests = new();
    public IReadOnlyCollection<ShiftExchangeRequest> ReceivedExchangeRequests => _receivedExchangeRequests.AsReadOnly();

    // Статический словарь для получения времени смен
    private static readonly Dictionary<ShiftType, (TimeSpan Start, TimeSpan End)> ShiftHours = new()
    {
        [ShiftType.Morning] = (TimeSpan.FromHours(10), TimeSpan.FromHours(14)),
        [ShiftType.Day] = (TimeSpan.FromHours(14), TimeSpan.FromHours(18)),
        [ShiftType.Evening] = (TimeSpan.FromHours(18), TimeSpan.FromHours(22)),
        [ShiftType.FullDay] = (TimeSpan.FromHours(13), TimeSpan.FromHours(22))
    };

    public Shift(
        Guid employeeId,
        DateTime date,
        ShiftType type,
        string? notes = null)
    {
        if (!ShiftHours.ContainsKey(type))
            throw new ArgumentException($"Unknown shift type: {type}", nameof(type));

        var hours = ShiftHours[type];
        StartTime = date.Date + hours.Start;
        EndTime = date.Date + hours.End;

        if (StartTime >= EndTime)
            throw new ArgumentException("Start time must be before end time");

        if (StartTime < DateTime.UtcNow.Date)
            throw new ArgumentException("Cannot create shift in the past");

        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        Type = type;
        Status = ShiftStatus.Scheduled;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    // Альтернативный конструктор для кастомного времени
    public Shift(
        Guid employeeId,
        DateTime startTime,
        DateTime endTime,
        ShiftType type,
        string? notes = null)
    {
        if (startTime >= endTime)
            throw new ArgumentException("Start time must be before end time");

        if (startTime < DateTime.UtcNow.Date)
            throw new ArgumentException("Cannot create shift in the past");

        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        StartTime = startTime;
        EndTime = endTime;
        Type = type;
        Status = ShiftStatus.Scheduled;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
        if (Status != ShiftStatus.Scheduled)
            throw new InvalidOperationException($"Cannot start shift with status {Status}");

        if (StartTime > DateTime.UtcNow)
            throw new InvalidOperationException("Cannot start shift before its scheduled time");

        Status = ShiftStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status != ShiftStatus.InProgress && Status != ShiftStatus.Scheduled)
            throw new InvalidOperationException($"Cannot complete shift with status {Status}");

        if (EndTime > DateTime.UtcNow && Status == ShiftStatus.Scheduled)
            throw new InvalidOperationException("Cannot complete shift that hasn't started yet");

        Status = ShiftStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (StartTime <= DateTime.UtcNow)
            throw new InvalidOperationException("Cannot cancel shift that has already started");

        Status = ShiftStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public static TimeSpan GetShiftDuration(ShiftType type)
    {
        var hours = ShiftHours[type];
        return hours.End - hours.Start;
    }

    public static (TimeSpan Start, TimeSpan End) GetShiftHours(ShiftType type)
    {
        return ShiftHours[type];
    }
}