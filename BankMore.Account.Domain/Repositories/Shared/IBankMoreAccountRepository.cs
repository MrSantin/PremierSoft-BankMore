using BankMore.Account.Domain.Entities.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Domain.Repositories.Shared;

public interface IBankMoreAccountRepository<T> where T : class, IEntity
{
    Task<T?> GetAsync(Guid id, CancellationToken ct);
    Task<T> UpdateAsync(T entity, CancellationToken ct);
    Task<T?> DeleteAsync(Guid id, CancellationToken ct);
    Task<T> CreateAsync(T entity, CancellationToken ct);
}

