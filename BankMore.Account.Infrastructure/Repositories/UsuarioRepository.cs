using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using BankMore.Account.Infrastructure.DbContexts;
using BankMore.Account.Infrastructure.Repositories.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BankMore.Account.Infrastructure.Repositories;

public class UsuarioRepository : BankMoreAccountRepository<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(BankMoreAccountContext context) : base(context)
    {
    }

    public async Task<Usuario?> GetByCpfAsync(string cpf, CancellationToken ct)
    {
        return await _dbSet.AsNoTracking()
                           .Include(x => x.ContaCorrente)
                           .FirstOrDefaultAsync(u => u.Cpf == cpf);
    }
}

