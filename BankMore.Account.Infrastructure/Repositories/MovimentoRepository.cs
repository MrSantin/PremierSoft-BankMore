using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using BankMore.Account.Infrastructure.DbContexts;
using BankMore.Account.Infrastructure.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace BankMore.Account.Infrastructure.Repositories;

public class MovimentoRepository : BankMoreAccountRepository<Movimento>, IMovimentoRepository
{
    public MovimentoRepository(BankMoreAccountContext context) : base(context)
    {
    }

    public async Task<decimal> GetSaldoAsync(Guid idConta, CancellationToken ct)
    {
        var movimentacoes = await _dbSet.AsNoTracking()
                           .Where(x => x.IdContaCorrente == idConta).ToListAsync();
        if (!movimentacoes.Any())
            return Math.Round(Convert.ToDecimal(0), 2);

        var saldo = movimentacoes.Sum(x => x.TipoMovimento == TipoMovimento.Credito ? x.Valor : -x.Valor);

        return Math.Round(Convert.ToDecimal(saldo), 2); ;
    }
}

