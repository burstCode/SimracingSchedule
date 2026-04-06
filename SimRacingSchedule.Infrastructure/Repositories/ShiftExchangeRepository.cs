using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Core.Interfaces;
using SimRacingSchedule.Infrastructure.Data;

namespace SimRacingSchedule.Infrastructure.Repositories;

public class ShiftExchangeRepository : Repository<ShiftExchangeRequest>, IShiftExchangeRepository
{
    public ShiftExchangeRepository(ApplicationDbContext context)
        : base(context) { }

    public async Task<IEnumerable<ShiftExchangeRequest>> GetPendingRequestsForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Include(r => r.Requester)
            .Include(r => r.Target)
            .Include(r => r.RequesterShift)
            .Include(r => r.TargetShift)
            .Where(r => r.TargetId == employeeId && r.Status == ExchangeRequestStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ShiftExchangeRequest>> GetRequestsByRequesterAsync(Guid requesterId, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Include(r => r.Requester)
            .Include(r => r.Target)
            .Include(r => r.RequesterShift)
            .Include(r => r.TargetShift)
            .Where(r => r.RequesterId == requesterId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ShiftExchangeRequest>> GetRequestsByTargetAsync(Guid targetId, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Include(r => r.Requester)
            .Include(r => r.Target)
            .Include(r => r.RequesterShift)
            .Include(r => r.TargetShift)
            .Where(r => r.TargetId == targetId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<ShiftExchangeRequest>> GetByStatusAsync(ExchangeRequestStatus status, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Include(r => r.Requester)
            .Include(r => r.Target)
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ShiftExchangeRequest?> GetPendingRequestBetweenEmployeesAsync(Guid requesterId, Guid targetId, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .FirstOrDefaultAsync(r =>
                r.RequesterId == requesterId &&
                r.TargetId == targetId &&
                r.Status == ExchangeRequestStatus.Pending,
                cancellationToken).ConfigureAwait(false);
    }

    public override async Task<ShiftExchangeRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Include(r => r.Requester)
            .Include(r => r.Target)
            .Include(r => r.RequesterShift)
            .Include(r => r.TargetShift)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }
}
