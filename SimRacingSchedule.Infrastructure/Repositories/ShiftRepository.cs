using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Core.Interfaces;
using SimRacingSchedule.Infrastructure.Data;

namespace SimRacingSchedule.Infrastructure.Repositories;

public class ShiftRepository : Repository<Shift>, IShiftRepository
{
    public ShiftRepository(ApplicationDbContext context)
        : base(context) { }

    public async Task<IEnumerable<Shift>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Where(s => s.EmployeeId == employeeId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Shift>> GetByEmployeeIdAsync(Guid employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Where(s => s.EmployeeId == employeeId &&
                        s.StartTime >= startDate &&
                        s.StartTime <= endDate)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Shift>> GetAvailableForExchangeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Where(s => s.EmployeeId == employeeId &&
                        s.Status == ShiftStatus.Scheduled &&
                        s.StartTime > DateTime.UtcNow)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Shift>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        DateTime startOfDay = date.Date;
        DateTime endOfDay = date.Date.AddDays(1);

        return await this.m_DbSet
            .Include(s => s.Employee)
            .Where(s => s.StartTime >= startOfDay && s.StartTime < endOfDay)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Shift>> GetByStatusAsync(ShiftStatus status, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Where(s => s.Status == status)
            .Include(s => s.Employee)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasConflictAsync(Guid employeeId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet.AnyAsync(s =>
            s.EmployeeId == employeeId &&
            s.Status == ShiftStatus.Scheduled &&
            s.StartTime < endTime &&
            s.EndTime > startTime,
            cancellationToken)
            .ConfigureAwait(false);
    }

    public override async Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Include(s => s.Employee)
            .Include(s => s.SentExchangeRequests)
            .Include(s => s.ReceivedExchangeRequests)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }
}
