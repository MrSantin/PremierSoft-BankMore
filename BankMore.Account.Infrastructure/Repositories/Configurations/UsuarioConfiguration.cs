using BankMore.Account.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BankMore.Account.Infrastructure.Repositories.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(x => x.IdUsuario);
        builder.HasIndex(x => x.Cpf).IsUnique();
        builder.Property(x => x.Nome).IsRequired();
        builder.Property(x => x.Cpf).IsRequired();
        builder.Property(x => x.Senha).IsRequired();
        builder.HasOne(x => x.ContaCorrente)
                .WithOne(x => x.Usuario)
                .HasForeignKey<ContaCorrente>(x => x.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);
    }
}

