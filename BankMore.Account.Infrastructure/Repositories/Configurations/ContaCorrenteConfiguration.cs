using BankMore.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankMore.Account.Infrastructure.Repositories.Configurations;

public sealed class ContaCorrenteConfiguration : IEntityTypeConfiguration<ContaCorrente>
{
    public void Configure(EntityTypeBuilder<ContaCorrente> builder)
    {
        builder.ToTable("contacorrente");

        builder.HasKey(x => x.IdContaCorrente);

        builder.Property(x => x.IdContaCorrente).HasColumnName("idcontacorrente").HasMaxLength(37).IsRequired();


        builder.Property(x => x.Numero).HasColumnName("numero")
            .UseIdentityColumn(100000, 1) //coloquei o valor inicial como 100000 ficar mais parecido com o padrão de contas bancárias, como não será utilizado em larga escala, não vejo problema em iniciar por esse número
            .ValueGeneratedOnAdd().IsRequired();

        builder.HasIndex(x => x.Numero).IsUnique();

        builder.Property(x => x.Nome).HasColumnName("nome").HasMaxLength(100).IsRequired();

        builder.Property(x => x.Ativo).HasColumnName("ativo").IsRequired().HasDefaultValue(true);

        builder.Property(x => x.Senha).HasColumnName("senha").HasMaxLength(100).IsRequired();

        builder.Property(x => x.Salt).HasColumnName("salt").HasMaxLength(100).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("CK_Ativo_Boolean", "ativo IN (0, 1)"));
        builder.Property(x => x.IdUsuario).IsRequired();
        builder.HasIndex(x => x.IdUsuario).IsUnique();
    }
}
