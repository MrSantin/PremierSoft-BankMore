using BankMore.Transfer.Domain.Entities;
using BankMore.Transfer.Domain.Repositories;
using BankMore.Transfer.Infrastructure.DbContexts;
using BankMore.Transfer.Infrastructure.Repositories.Shared;


namespace BankMore.Transfer.Infrastructure.Repositories
{
    public class TranferenciaRepository : BankMoreTransferRepository<Transferencia>, ITransferenciaRepository
    {
        public TranferenciaRepository(BankMoreTransferContext context) : base(context)
        {
        }
    }
}
