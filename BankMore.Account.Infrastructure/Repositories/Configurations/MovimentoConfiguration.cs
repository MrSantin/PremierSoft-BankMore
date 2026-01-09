using BankMore.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankMore.Account.Infrastructure.Repositories.Configurations;

public sealed class MovimentoConfiguration : IEntityTypeConfiguration<Movimento>
{
    public void Configure(EntityTypeBuilder<Movimento> builder)
    {
        builder.ToTable("movimento");

        builder.HasKey(x => x.IdMovimento);

        builder.Property(x => x.IdMovimento).HasColumnName("idmovimento").IsRequired();

        builder.Property(x => x.IdContaCorrente).HasColumnName("idcontacorrente").IsRequired();

        builder.Property(x => x.DataMovimento).HasColumnName("datamovimento").HasColumnType("datetime").IsRequired();

        builder.Property(x => x.TipoMovimento).HasColumnName("tipomovimento")
            .HasConversion(
                v => v == TipoMovimento.Credito ? "C" : "D",
                v => v == "C" ? TipoMovimento.Credito : TipoMovimento.Debito)
            .HasMaxLength(1)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.Valor).HasColumnName("valor").HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(x => x.ContaCorrente).WithMany().HasForeignKey(x => x.IdContaCorrente).OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint("CK_TipoMovimento", "tipomovimento IN ('C', 'D')"));
    }
}
