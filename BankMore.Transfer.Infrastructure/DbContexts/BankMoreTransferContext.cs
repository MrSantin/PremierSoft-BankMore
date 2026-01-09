using BankMore.Transfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankMore.Transfer.Infrastructure.DbContexts;

public partial class BankMoreTransferContext : DbContext
{
    public BankMoreTransferContext(DbContextOptions<BankMoreTransferContext> options) : base(options)
    {

    }
    public virtual DbSet<Transferencia> Transferencias { get; set; }
    public virtual DbSet<Idempotencia> Idempotencias { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankMoreTransferContext).Assembly);
    }
}

