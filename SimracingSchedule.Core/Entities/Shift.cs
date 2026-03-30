using SimRacingSchedule.Core.Exceptions;

namespace SimRacingSchedule.Core.Entities;

/// <summary>
/// Рабочая смена.
/// </summary>
public class Shift
{
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public ShiftType Type { get; private set; }
    public ShiftStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private Shift() { }

    public Shift(
        Guid id,
        Guid employeeId,
        DateTime startTime,
        DateTime endTime,
        ShiftType type,
        string? notes = null)
    {
        if (startTime >= endTime)
            throw new DomainException("Время начала смены должно быть раньше ее окончания");

        if (startTime < DateTime.UtcNow.Date)
            throw new DomainException("Нельзя создать смену в прошлом");

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        EmployeeId = employeeId;
        StartTime = startTime;
        EndTime = endTime;
        Type = type;
        Status = ShiftStatus.Scheduled;
        Notes = notes;
    }

    public void Cancel()
    {
        if (StartTime <= DateTime.UtcNow)
            throw new DomainException("Невозможно отменить смену, которая уже началась");

        Status = ShiftStatus.Cancelled;
    }

    public void Complete()
    {
        if (EndTime > DateTime.UtcNow)
            throw new DomainException($"Невозможно закрыть смену раньше, чем в {EndTime}");

        Status = ShiftStatus.Completed;
    }
}
