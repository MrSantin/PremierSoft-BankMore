using BankMore.Transfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankMore.Transfer.Infrastructure.Repositories.Configurations;

public sealed class TransferenciaConfiguration : IEntityTypeConfiguration<Transferencia>
{
    public void Configure(EntityTypeBuilder<Transferencia> builder)
    {
        builder.ToTable("transferencia");

        builder.HasKey(x => x.IdTransferencia);

        builder.Property(x => x.IdTransferencia).HasColumnName("idtransferencia").IsRequired();

        builder.Property(x => x.IdContaOrigem).HasColumnName("idcontacorrente_origem").IsRequired();
        builder.Property(x => x.IdContaDestino).HasColumnName("idcontacorrente_destino").IsRequired();
        builder.Property(x => x.DataMovimento).HasColumnName("datamovimento").HasColumnType("datetime").IsRequired();
        builder.Property(x => x.Valor).HasColumnName("valor").HasColumnType("decimal(18,2)").IsRequired();
        
        //Acredito que essa referencia seria para identificar movimentações de estorno, caso houvesse falhas nas transferencias
        //Como fiz as duas operações em uma transação atômica, não vejo a necessidade de haver esse auto relacionamento

        //builder.HasOne<Transferencia>()
        //       .WithOne()
        //       .HasForeignKey<Transferencia>(x => x.IdTransferencia)
        //       .HasPrincipalKey<Transferencia>(x => x.IdTransferencia)
        //       .OnDelete(DeleteBehavior.NoAction);
    }
}