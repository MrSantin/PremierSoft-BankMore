using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using BankMore.Account.Infrastructure.DbContexts;
using BankMore.Account.Infrastructure.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace BankMore.Account.Infrastructure.Repositories;

public class ContaCorrenteRepository : BankMoreAccountRepository<ContaCorrente>, IContaCorrenteRepository
{
    public ContaCorrenteRepository(BankMoreAccountContext context) : base(context)
    {
    }


    public async Task<ContaCorrente?> GetByNumeroContaAsync(int numeroConta, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
                           .Include(x => x.Usuario)
                           .FirstOrDefaultAsync(u => u.Numero == numeroConta);
    }
}

