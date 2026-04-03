using SimRacingSchedule.Core.Enums;

namespace SimRacingSchedule.Core.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName(this ShiftType shiftType)
    {
        return shiftType switch
        {
            ShiftType.Morning => "Утренняя (10:00-14:00)",
            ShiftType.Day => "Дневная (14:00-18:00)",
            ShiftType.Evening => "Вечерняя (18:00-22:00)",
            ShiftType.FullDay => "Полный день (13:00-22:00)",
            _ => shiftType.ToString()
        };
    }

    public static string GetDisplayName(this ShiftStatus status)
    {
        return status switch
        {
            ShiftStatus.Scheduled => "Запланирована",
            ShiftStatus.InProgress => "В процессе",
            ShiftStatus.Completed => "Завершена",
            ShiftStatus.Cancelled => "Отменена",
            _ => status.ToString()
        };
    }

    public static string GetDisplayName(this ExchangeRequestStatus status)
    {
        return status switch
        {
            ExchangeRequestStatus.Pending => "Ожидает",
            ExchangeRequestStatus.Approved => "Одобрен",
            ExchangeRequestStatus.Rejected => "Отклонен",
            ExchangeRequestStatus.Cancelled => "Отменен",
            _ => status.ToString()
        };
    }

    public static string GetDisplayName(this EmployeeRole role)
    {
        return role switch
        {
            EmployeeRole.Employee => "Сотрудник",
            EmployeeRole.Administrator => "Администратор",
            _ => role.ToString()
        };
    }
}
