namespace SimRacingSchedule.Core.Enums;

/// <summary>
/// Кому принадлежит смена.
/// </summary>
public enum ShiftSource
{
    /// <summary>
    /// Себе.
    /// </summary>
    Own = 0,

    /// <summary>
    /// Получена.
    /// </summary>
    Received = 1,

    /// <summary>
    /// Отдана.
    /// </summary>
    Given = 2,
}
