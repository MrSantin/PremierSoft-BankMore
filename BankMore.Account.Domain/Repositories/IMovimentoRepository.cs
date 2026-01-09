using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Domain.Repositories;

public interface IMovimentoRepository : IBankMoreAccountRepository<Movimento>
{
    Task<decimal> GetSaldoAsync(Guid idConta, CancellationToken ct);
}

