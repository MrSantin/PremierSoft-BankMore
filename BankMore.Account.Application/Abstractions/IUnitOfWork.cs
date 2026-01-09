

using BankMore.Account.Application.Shared;

namespace BankMore.Account.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<TransactionResult> ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct);
}
