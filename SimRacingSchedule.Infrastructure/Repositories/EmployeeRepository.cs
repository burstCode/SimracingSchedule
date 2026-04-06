using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Core.Entities;
using SimRacingSchedule.Core.Enums;
using SimRacingSchedule.Core.Interfaces;
using SimRacingSchedule.Infrastructure.Data;

namespace SimRacingSchedule.Infrastructure.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(ApplicationDbContext context)
        : base(context) { }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Employee?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .FirstOrDefaultAsync(e => e.PhoneNumber == phoneNumber, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Where(e => e.IsActive)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Employee>> GetByRoleAsync(EmployeeRole role, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Where(e => e.Role == role && e.IsActive)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await this.m_DbSet.AnyAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);

    public override async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet
            .Include(e => e.Shifts)
            .Include(e => e.SentExchangeRequests)
                .ThenInclude(r => r.TargetShift)
            .Include(e => e.ReceivedExchangeRequests)
                .ThenInclude(r => r.RequesterShift)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }
}
