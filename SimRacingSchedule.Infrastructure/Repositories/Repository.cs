using Microsoft.EntityFrameworkCore;
using SimRacingSchedule.Core.Interfaces;
using SimRacingSchedule.Infrastructure.Data;

namespace SimRacingSchedule.Infrastructure.Repositories;

public abstract class Repository<T> : IRepository<T>
    where T : class
{
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable IDE1006 // Naming Styles
    protected readonly ApplicationDbContext m_Context;
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable IDE1006 // Naming Styles
    protected readonly DbSet<T> m_DbSet;
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1401 // Fields should be private
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    protected Repository(ApplicationDbContext context)
    {
        this.m_Context = context;
        this.m_DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await this.m_DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default) => await this.m_DbSet.ToListAsync(cancellationToken).ConfigureAwait(false);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _ = await this.m_DbSet.AddAsync(entity, cancellationToken);
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _ = this.m_DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _ = this.m_DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _ = await this.m_Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
