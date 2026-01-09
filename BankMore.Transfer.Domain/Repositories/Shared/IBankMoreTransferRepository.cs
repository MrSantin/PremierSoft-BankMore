using BankMore.Transfer.Domain.Entities.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Transfer.Domain.Repositories.Shared;

public interface IBankMoreTransferRepository<T> where T : class, IEntity
{
    Task<T?> GetAsync(Guid id, CancellationToken ct);
    Task<T> UpdateAsync(T entity, CancellationToken ct);
    Task<T?> DeleteAsync(Guid id, CancellationToken ct);
    Task<T> CreateAsync(T entity, CancellationToken ct);
}
