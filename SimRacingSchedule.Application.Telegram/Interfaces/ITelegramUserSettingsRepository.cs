using SimRacingSchedule.Application.Telegram.Models;

namespace SimRacingSchedule.Application.Telegram.Interfaces;

public interface ITelegramUserSettingsRepository
{
    Task<TelegramUserSettings?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct = default);
    Task<TelegramUserSettings?> GetByChatIdAsync(long chatId, CancellationToken ct = default);
    Task<IEnumerable<TelegramUserSettings>> GetAllEnabledAsync(CancellationToken ct = default);
    Task AddAsync(TelegramUserSettings settings, CancellationToken ct = default);
    Task UpdateAsync(TelegramUserSettings settings, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
