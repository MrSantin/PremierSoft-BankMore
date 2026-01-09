using BankMore.Transfer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankMore.Transfer.Infrastructure.Repositories.Configurations;

public sealed class IdempotenciaConfiguration : IEntityTypeConfiguration<Idempotencia>
{
    public void Configure(EntityTypeBuilder<Idempotencia> builder)
    {
        builder.ToTable("idempotencia");

        builder.HasKey(x => x.ChaveIdempotencia);

        builder.Property(x => x.ChaveIdempotencia).HasColumnName("chave_idempotencia").IsRequired();

        builder.Property(x => x.Requisicao).HasColumnName("requisicao").HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.Resultado).HasColumnName("resultado").HasMaxLength(1000).IsRequired(false);
    }
}

