namespace SimRacingSchedule.Core;

public enum EmployeeRole
{
    Employee,
    Administrator
}

public enum ShiftType
{
    // ТРЦ Малибу работает с 10:00 до 21:00,
    // но клуб работает до 22:00.
    // Поэтому делаем задел на будущее -
    // если появятся работники на ставку 0.5.
    Morning,    // 10:00 -> 14:00
    Day,        // 14:00 -> 18:00
    Evening,    // 18:00 -> 22:00

    // Обычный режим работы клуба ниже.
    FullDay,    // 13:00 -> 22:00

}


public enum ShiftStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public enum ExchangeRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Cancelled
}
