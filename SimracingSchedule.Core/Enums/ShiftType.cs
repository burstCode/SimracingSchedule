namespace SimRacingSchedule.Core.Enums;

/// <summary>
/// Тип смены.
/// </summary>
public enum ShiftType
{
    /// <summary>
    /// ТРЦ Малибу работает с 10:00 до 21:00,
    /// но клуб работает до 22:00.
    /// Поэтому делаем задел на будущее -
    /// если появятся работники на ставку 0.5.
    /// </summary>
    Morning,    // 10:00 -> 14:00

    /// <summary>
    /// Дневная смена на 0.5 ставки.
    /// </summary>
    Day,        // 14:00 -> 18:00

    /// <summary>
    /// Вечерняя смена на 0.5 ставки.
    /// </summary>
    Evening,    // 18:00 -> 22:00

    /// <summary>
    /// Полный рабочий день (8-9 часов)
    /// </summary>
    FullDay,    // 13:00 -> 22:00
}
