using BankMore.Account.Domain.Entities.Shared;
using BankMore.Account.Domain.Repositories.Shared;
using BankMore.Account.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace BankMore.Account.Infrastructure.Repositories.Shared;

public class BankMoreAccountRepository<T> : IBankMoreAccountRepository<T>
where T : class, IEntity
{
    protected readonly BankMoreAccountContext _context;
    protected readonly DbSet<T> _dbSet;

    public BankMoreAccountRepository(BankMoreAccountContext context)
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

