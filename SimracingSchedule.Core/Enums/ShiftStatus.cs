namespace SimRacingSchedule.Core.Enums;

/// <summary>
/// Статус смены.
/// </summary>
public enum ShiftStatus
{
    /// <summary>
    /// Запланирована.
    /// </summary>
    Scheduled,

    /// <summary>
    /// В процессе.
    /// </summary>
    InProgress,

    /// <summary>
    /// Завершена.
    /// </summary>
    Completed,

    /// <summary>
    /// Отменена.
    /// </summary>
    Cancelled,
}
