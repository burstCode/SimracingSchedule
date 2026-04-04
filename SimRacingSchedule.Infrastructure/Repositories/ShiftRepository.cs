using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Core.Interfaces;
using SimRacingSchedule.Infrastructure.Data;

namespace SimRacingSchedule.Infrastructure.Repositories;

public class ShiftRepository : Repository<Shift>, IShiftRepository
{
    public ShiftRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<Shift>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.EmployeeId == employeeId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Shift>> GetByEmployeeIdAsync(Guid employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.EmployeeId == employeeId &&
                        s.StartTime >= startDate &&
                        s.StartTime <= endDate)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Shift>> GetAvailableForExchangeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.EmployeeId == employeeId &&
                        s.Status == ShiftStatus.Scheduled &&
                        s.StartTime > DateTime.UtcNow)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Shift>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = date.Date.AddDays(1);

        return await _dbSet
            .Include(s => s.Employee)
            .Where(s => s.StartTime >= startOfDay && s.StartTime < endOfDay)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Shift>> GetByStatusAsync(ShiftStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.Status == status)
            .Include(s => s.Employee)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasConflictAsync(Guid employeeId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(s =>
            s.EmployeeId == employeeId &&
            s.Status == ShiftStatus.Scheduled &&
            s.StartTime < endTime &&
            s.EndTime > startTime,
            cancellationToken);
    }

    public override async Task<Shift?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Employee)
            .Include(s => s.SentExchangeRequests)
            .Include(s => s.ReceivedExchangeRequests)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }
}
