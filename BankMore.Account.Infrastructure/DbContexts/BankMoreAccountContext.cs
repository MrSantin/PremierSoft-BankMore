using BankMore.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Infrastructure.DbContexts;

public partial class BankMoreAccountContext : DbContext
{
    public BankMoreAccountContext(DbContextOptions<BankMoreAccountContext> options) : base(options)
    {

    }
    public virtual DbSet<ContaCorrente> ContasCorrente { get; set; }
    public virtual DbSet<Movimento> Movimentos { get; set; }
    public virtual DbSet<Idempotencia> Idempotencias { get; set; }
    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BankMoreAccountContext).Assembly);
    }
}

