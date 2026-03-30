using SimRacingSchedule.Core.ValueObjects;

namespace SimRacingSchedule.Core.Entities;

/// <summary>
/// Сотрудник симрейсинг-клуба.
/// </summary>
public class Employee
{
    private readonly List<Shift> _shifts = new();
    private readonly List<ShiftExchangeRequest> _sentExchangeRequests = new();
    private readonly List<ShiftExchangeRequest> _receivedExchangeRequests = new();

    public Guid Id { get; private set; }
    public FullName FullName { get; private set; }
    public Email Email { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; }
    public Position Position { get; private set; }
    public EmployeeRole EmployeeRole { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Shift> Shifts => _shifts.AsReadOnly();
    public IReadOnlyCollection<ShiftExchangeRequest> SentExchangeRequests => _sentExchangeRequests.AsReadOnly();
    public IReadOnlyCollection<ShiftExchangeRequest> ReceivedExchangeRequests => _receivedExchangeRequests.AsReadOnly();

    public Employee(
        Guid id,
        FullName fullName,
        Email email,
        PhoneNumber phoneNumber,
        Position position,
        EmployeeRole employeeRole = EmployeeRole.Employee)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        FullName = fullName ?? throw new ArgumentException(nameof(fullName));
        Email = email ?? throw new ArgumentException(nameof(email));
        PhoneNumber = phoneNumber ?? throw new ArgumentException(nameof(phoneNumber));
        Position = position ?? throw new ArgumentException(nameof(position));
        EmployeeRole = employeeRole;
        IsActive = true;
    }

    public void UpdateProfile(FullName fullName, Email email, PhoneNumber phoneNumber)
    {
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
    }

    public void ChangePosition(Position newPosition)
    {
        Position = newPosition;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public bool CanExchangeShift(Shift shift, Employee targetEmployee)
    {
        if (!IsActive || !targetEmployee.IsActive)
            return false;

        if (shift.EmployeeId != Id)
            return false;

        if (shift.StartTime <= DateTime.UtcNow)
            return false;

        bool targetHasConflict = targetEmployee.Shifts.Any(s =>
            s.StartTime < shift.EndTime && s.EndTime > shift.StartTime);

        return !targetHasConflict;
    }
}
