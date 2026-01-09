using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories.Shared;

namespace BankMore.Account.Domain.Repositories;

public interface IContaCorrenteRepository : IBankMoreAccountRepository<ContaCorrente>
{
    Task<ContaCorrente?> GetByNumeroContaAsync(int numeroConta, CancellationToken ct);
}

