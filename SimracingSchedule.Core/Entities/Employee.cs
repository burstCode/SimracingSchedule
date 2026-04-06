using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Entities;

/// <summary>
/// Объект сотрудника.
/// </summary>
public class Employee
{
    private readonly List<Shift> m_Shifts = new ();
    private readonly List<ShiftExchangeRequest> m_SentExchangeRequests = new ();
    private readonly List<ShiftExchangeRequest> m_ReceivedExchangeRequests = new ();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Employee"/>.
    /// </summary>
    /// <param name="firstName">Имя сотрудника.</param>
    /// <param name="lastName">Фамилия сотрудника.</param>
    /// <param name="email">Адрес электронной почты сотрудника.</param>
    /// <param name="phoneNumber">Номер телефона сотрудника.</param>
    /// <param name="position">Должность сотрудника.</param>
    /// <param name="role">Роль сотрудника.</param>
    public Employee(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        string position,
        EmployeeRole role = EmployeeRole.Employee)
    {
        this.Id = Guid.NewGuid();
        this.FirstName = firstName ?? throw new ArgumentNullException(nameof(firstName));
        this.LastName = lastName ?? throw new ArgumentNullException(nameof(lastName));
        this.Email = email ?? throw new ArgumentNullException(nameof(email));
        this.PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        this.Position = position ?? throw new ArgumentNullException(nameof(position));
        this.Role = role;
        this.IsActive = true;
        this.CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Получает идентификатор сотрудника.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Получает имя сотрудника.
    /// </summary>
    public string FirstName { get; private set; }

    /// <summary>
    /// Получает фамилию сотрудника.
    /// </summary>
    public string LastName { get; private set; }

    /// <summary>
    /// Получает отчество сотрудника.
    /// </summary>
    public string? Patronymic { get; private set; }

    /// <summary>
    /// Получает адрес электронной почты сотрудника.
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// Получает номер телефона сотрудника.
    /// </summary>
    public string PhoneNumber { get; private set; }

    /// <summary>
    /// Получает должность сотрудника.
    /// </summary>
    public string Position { get; private set; }

    /// <summary>
    /// Получает роль сотрудника.
    /// </summary>
    public EmployeeRole Role { get; private set; }

    /// <summary>
    /// Получает значение, показывающее, работает ли сотрудник в клубе.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Получает дату создания.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Получает дату обновления.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Получает смены сотрудника.
    /// </summary>
    public IReadOnlyCollection<Shift> Shifts => this.m_Shifts.AsReadOnly();

    /// <summary>
    /// Получает список отправленных предложений об обмене сменами.
    /// </summary>
    public IReadOnlyCollection<ShiftExchangeRequest> SentExchangeRequests => this.m_SentExchangeRequests.AsReadOnly();

    /// <summary>
    /// Получает список полученных предложений об обмене сменами.
    /// </summary>
    public IReadOnlyCollection<ShiftExchangeRequest> ReceivedExchangeRequests => this.m_ReceivedExchangeRequests.AsReadOnly();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Employee"/>.
    /// </summary>
    /// <param name="firstName">Имя сотрудника.</param>
    /// <param name="lastName">Фамилия сотрудника.</param>
    /// <param name="patronymic">Отчество сотрудника.</param>
    /// <param name="email">Адрес электронной почты сотрудника.</param>
    /// <param name="phoneNumber">Номер телефона сотрудника.</param>
    /// <param name="position">Должность сотрудника.</param>
    public void Update(
        string firstName,
        string lastName,
        string? patronymic,
        string email,
        string phoneNumber,
        string position)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.Patronymic = patronymic;
        this.Email = email;
        this.PhoneNumber = phoneNumber;
        this.Position = position;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Изменить роль сотрудника.
    /// </summary>
    /// <param name="newRole">Новая роль.</param>
    public void ChangeRole(EmployeeRole newRole)
    {
        this.Role = newRole;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Активировать сотрудника.
    /// </summary>
    public void Activate()
    {
        this.IsActive = true;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отключить сотрудника.
    /// </summary>
    public void Deactivate()
    {
        this.IsActive = false;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Проверить на предмет доступности обмены сменами.
    /// </summary>
    /// <param name="shift">Смена, на которую сотрудник может обменять свою.</param>
    /// <param name="targetEmployee">Сотрудник для обмена.</param>
    /// <returns>Истина/ложь.</returns>
    public bool CanExchangeShift(Shift shift, Employee targetEmployee)
    {
        if (!this.IsActive || !targetEmployee.IsActive)
        {
            return false;
        }

        if (shift.EmployeeId != this.Id)
        {
            return false;
        }

        if (shift.StartTime <= DateTime.UtcNow)
        {
            return false;
        }

        bool targetHasConflict = targetEmployee.Shifts.Any(s =>
            s.Status == ShiftStatus.Scheduled &&
            s.StartTime < shift.EndTime &&
            s.EndTime > shift.StartTime);

        return !targetHasConflict;
    }
}
