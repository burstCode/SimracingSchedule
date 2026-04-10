using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Application.Telegram.Interfaces;
using SimRacingSchedule.Application.Telegram.Models;
using SimRacingSchedule.Infrastructure.Data;

namespace SimRacingSchedule.Infrastructure.Repositories;

public class TelegramUserSettingsRepository : ITelegramUserSettingsRepository
{
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    private readonly ApplicationDbContext m_Context;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    public TelegramUserSettingsRepository(ApplicationDbContext context)
    {
        this.m_Context = context;
    }

    public async Task<TelegramUserSettings?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken ct = default)
    {
        return await this.m_Context.Set<TelegramUserSettings>()
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId, ct).ConfigureAwait(false);
    }

    public async Task<TelegramUserSettings?> GetByChatIdAsync(long chatId, CancellationToken ct = default)
    {
        return await this.m_Context.Set<TelegramUserSettings>()
            .FirstOrDefaultAsync(s => s.TelegramChatId == chatId, ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TelegramUserSettings>> GetAllEnabledAsync(CancellationToken ct = default)
    {
        return await this.m_Context.Set<TelegramUserSettings>()
            .Where(s => s.IsEnabled)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(TelegramUserSettings settings, CancellationToken ct = default)
    {
        await this.m_Context.Set<TelegramUserSettings>().AddAsync(settings, ct);
    }

    public Task UpdateAsync(TelegramUserSettings settings, CancellationToken ct = default)
    {
        this.m_Context.Set<TelegramUserSettings>().Update(settings);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await this.m_Context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
