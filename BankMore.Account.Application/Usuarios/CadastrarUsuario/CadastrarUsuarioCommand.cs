using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;

namespace BankMore.Account.Application.Usuarios.CadastrarUsuario;

public class CadastrarUsuarioCommand : IAccountCommand
{
    public Guid IdIdempotencia { get; set; } = default!;
    public string Nome { get; set; } = default!;
    public string Cpf { get; set; } = default!;
    public string Senha { get; set; } = default!;

}

