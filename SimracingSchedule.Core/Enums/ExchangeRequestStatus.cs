namespace SimRacingSchedule.Core.Enums;

/// <summary>
/// Статус предложения обмена.
/// </summary>
public enum ExchangeRequestStatus
{
    /// <summary>
    /// Рассматривается.
    /// </summary>
    Pending,

    /// <summary>
    /// Одобрено.
    /// </summary>
    Approved,

    /// <summary>
    /// Отклонено.
    /// </summary>
    Rejected,

    /// <summary>
    /// Отменено.
    /// </summary>
    Cancelled,
}
