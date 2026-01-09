using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using BankMore.Account.Infrastructure.DbContexts;
using BankMore.Account.Infrastructure.Repositories.Shared;


namespace BankMore.Account.Infrastructure.Repositories
{
    public class IdempotenciaRepository : BankMoreAccountRepository<Idempotencia>, IIdempotenciaRepository
    {
        public IdempotenciaRepository(BankMoreAccountContext context) : base(context)
        {
        }
    }
}
