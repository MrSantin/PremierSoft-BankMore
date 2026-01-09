using BankMore.Transfer.Domain.Entities.Shared;
using BankMore.Transfer.Domain.Repositories.Shared;
using BankMore.Transfer.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
namespace BankMore.Transfer.Infrastructure.Repositories.Shared;

public class BankMoreTransferRepository<T> : IBankMoreTransferRepository<T>
where T : class, IEntity
{
    protected readonly BankMoreTransferContext _context;
    protected readonly DbSet<T> _dbSet;

    public BankMoreTransferRepository(BankMoreTransferContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync([id], ct);

    public virtual async Task<T> CreateAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.FromResult(entity);
    }

    public virtual async Task<T?> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetAsync(id, ct);
        if (entity != null)
            _dbSet.Remove(entity);

        return entity;
    }
}
