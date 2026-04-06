// Copyright (c) SimRacing Club. All rights reserved.
using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Entities;

/// <summary>
/// Объект смены.
/// </summary>
public class Shift
{
    private readonly List<ShiftExchangeRequest> m_SentExchangeRequests = new ();
    private readonly List<ShiftExchangeRequest> m_ReceivedExchangeRequests = new ();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Shift"/>.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <param name="date">Дата начала смены.</param>
    /// <param name="type">Тип смены.</param>
    /// <param name="notes">Заметки.</param>
    public Shift(
        Guid employeeId,
        DateTime date,
        ShiftType type,
        string? notes = null)
    {
        Dictionary<ShiftType, (TimeSpan Start, TimeSpan End)> shiftHours = new ()
        {
            [ShiftType.Morning] = (TimeSpan.FromHours(10), TimeSpan.FromHours(14)),
            [ShiftType.Day] = (TimeSpan.FromHours(14), TimeSpan.FromHours(18)),
            [ShiftType.Evening] = (TimeSpan.FromHours(18), TimeSpan.FromHours(22)),
            [ShiftType.FullDay] = (TimeSpan.FromHours(13), TimeSpan.FromHours(22)),
        };

        if (!shiftHours.ContainsKey(type))
        {
            throw new ArgumentException($"Unknown shift type: {type}", nameof(type));
        }

        (TimeSpan Start, TimeSpan End) hours = shiftHours[type];
        this.StartTime = date.Date + hours.Start;
        this.EndTime = date.Date + hours.End;

        if (this.StartTime >= this.EndTime)
        {
            throw new ArgumentException("Start time must be before end time");
        }

        if (this.StartTime < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Cannot create shift in the past");
        }

        this.Id = Guid.NewGuid();
        this.EmployeeId = employeeId;
        this.Type = type;
        this.Status = ShiftStatus.Scheduled;
        this.Notes = notes;
        this.CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="Shift"/>.
    /// </summary>
    /// <param name="employeeId">Идентификатор сотрудника.</param>
    /// <param name="startTime">Время начала смены.</param>
    /// <param name="endTime">Время окончания смены.</param>
    /// <param name="type">Тип смены.</param>
    /// <param name="notes">Заметки.</param>
    public Shift(
        Guid employeeId,
        DateTime startTime,
        DateTime endTime,
        ShiftType type,
        string? notes = null)
    {
        if (startTime >= endTime)
        {
            throw new ArgumentException("Start time must be before end time");
        }

        if (startTime < DateTime.UtcNow.Date)
        {
            throw new ArgumentException("Cannot create shift in the past");
        }

        this.Id = Guid.NewGuid();
        this.EmployeeId = employeeId;
        this.StartTime = startTime;
        this.EndTime = endTime;
        this.Type = type;
        this.Status = ShiftStatus.Scheduled;
        this.Notes = notes;
        this.CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Получает идентификатор смены.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Получает идентификатор сотрудника.
    /// </summary>
    public Guid EmployeeId { get; private set; }

    /// <summary>
    /// Получает объект сотрудника.
    /// </summary>
    public Employee? Employee { get; }

    /// <summary>
    /// Получает время начала смены.
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Получает время окончания смены.
    /// </summary>
    public DateTime EndTime { get; private set; }

    /// <summary>
    /// Получает тип смены.
    /// </summary>
    public ShiftType Type { get; private set; }

    /// <summary>
    /// Получает статус смены.
    /// </summary>
    public ShiftStatus Status { get; private set; }

    /// <summary>
    /// Получает заметки.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Получает время создания.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Получает время обновления.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Получает коллекция отправленных запросов на обмен смены.
    /// </summary>
    public IReadOnlyCollection<ShiftExchangeRequest> SentExchangeRequests => this.m_SentExchangeRequests.AsReadOnly();

    /// <summary>
    /// Получает Получает коллекция полученных запросов на обмен смены.
    /// </summary>
    public IReadOnlyCollection<ShiftExchangeRequest> ReceivedExchangeRequests => this.m_ReceivedExchangeRequests.AsReadOnly();

    /// <summary>
    /// Начать смену.
    /// </summary>
    /// <exception cref="InvalidOperationException">Если смена уже начата или окончена.</exception>
    public void Start()
    {
        if (this.Status != ShiftStatus.Scheduled)
        {
            throw new InvalidOperationException($"Cannot start shift with status {this.Status}");
        }

        if (this.StartTime > DateTime.UtcNow)
        {
            throw new InvalidOperationException("Cannot start shift before its scheduled time");
        }

        this.Status = ShiftStatus.InProgress;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Начать смену.
    /// </summary>
    /// <exception cref="InvalidOperationException">Если смена уже окончена.</exception>
    public void Complete()
    {
        if (this.Status != ShiftStatus.InProgress && this.Status != ShiftStatus.Scheduled)
        {
            throw new InvalidOperationException($"Cannot complete shift with status {this.Status}");
        }

        if (this.EndTime > DateTime.UtcNow && this.Status == ShiftStatus.Scheduled)
        {
            throw new InvalidOperationException("Cannot complete shift that hasn't started yet");
        }

        this.Status = ShiftStatus.Completed;
        this.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Отменить смену.
    /// </summary>
    /// <exception cref="InvalidOperationException">Если смена уже началась.</exception>
    public void Cancel()
    {
        if (this.StartTime <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Cannot cancel shift that has already started");
        }

        this.Status = ShiftStatus.Cancelled;
        this.UpdatedAt = DateTime.UtcNow;
    }
}
