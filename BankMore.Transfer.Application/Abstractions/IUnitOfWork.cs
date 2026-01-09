using BankMore.Transfer.Application.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Transfer.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<TransactionResult> ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct);
}

