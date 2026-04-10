namespace SimRacingSchedule.Application.Telegram.Models;

public class TelegramUserSettings
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public long TelegramChatId { get; set; }
    public bool IsEnabled { get; set; }
    public int NotificationMinutesBefore { get; set; } // За сколько минут уведомлять
    public bool NotifyShiftStart { get; set; }         // Уведомлять о начале смены
    public bool NotifyShiftExchange { get; set; }      // Уведомлять об обмене сменами
    public bool NotifyShiftReminder { get; set; }      // Напоминание о смене
    public string? TimeZone { get; set; }              // Часовой пояс
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public TelegramUserSettings()
    {
        Id = Guid.NewGuid();
        IsEnabled = true;
        NotificationMinutesBefore = 60; // По умолчанию за час
        NotifyShiftStart = true;
        NotifyShiftExchange = true;
        NotifyShiftReminder = true;
        TimeZone = "Europe/Moscow";
        CreatedAt = DateTime.UtcNow;
    }
}
