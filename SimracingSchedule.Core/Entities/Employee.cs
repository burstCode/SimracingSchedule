using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Entities;

public class Employee
{
    public Guid Id { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? Patronymic { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public string Position { get; private set; }
    public EmployeeRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<Shift> _shifts = new();
    public IReadOnlyCollection<Shift> Shifts => _shifts.AsReadOnly();

    private readonly List<ShiftExchangeRequest> _sentExchangeRequests = new();
    public IReadOnlyCollection<ShiftExchangeRequest> SentExchangeRequests => _sentExchangeRequests.AsReadOnly();

    private readonly List<ShiftExchangeRequest> _receivedExchangeRequests = new();
    public IReadOnlyCollection<ShiftExchangeRequest> ReceivedExchangeRequests => _receivedExchangeRequests.AsReadOnly();

    public Employee(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string position,
        EmployeeRole role = EmployeeRole.Employee)
    {
        Id = Guid.NewGuid();
        FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        Position = position ?? throw new ArgumentNullException(nameof(position));
        Role = role;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(
        string firstName,
        string lastName,
        string? patronymic,
        string email,
        string phoneNumber,
        string position)
    {
        FirstName = firstName;
        LastName = lastName;
        Patronymic = patronymic;
        Email = email;
        PhoneNumber = phoneNumber;
        Position = position;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(EmployeeRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanExchangeShift(Shift shift, Employee targetEmployee)
    {
        if (!IsActive || !targetEmployee.IsActive)
            return false;

        if (shift.EmployeeId != Id)
            return false;

        if (shift.StartTime <= DateTime.UtcNow)
            return false;

        var targetHasConflict = targetEmployee.Shifts.Any(s =>
            s.Status == ShiftStatus.Scheduled &&
            s.StartTime < shift.EndTime &&
            s.EndTime > shift.StartTime);

        return !targetHasConflict;
    }
}
