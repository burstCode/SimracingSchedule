using SimRacingSchedule.Application.Telegram.Models;
using SimRacingSchedule.Core.Entities;

namespace SimRacingSchedule.Application.Telegram.Services;

public interface ITelegramNotificationService
{
    Task SendShiftReminderAsync(Shift shift, Employee employee, int minutesBefore, CancellationToken ct = default);
    Task SendShiftExchangeNotificationAsync(ShiftExchangeRequest request, string action, CancellationToken ct = default);
    Task SendWelcomeMessageAsync(long chatId, string employeeName, CancellationToken ct = default);
    Task SendSettingsMessageAsync(long chatId, TelegramUserSettings settings, CancellationToken ct = default);
}
