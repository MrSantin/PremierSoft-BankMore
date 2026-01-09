using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;

namespace BankMore.Account.Application.Conta.CadastrarConta;

public class CadastrarContaCommand : IAccountCommand
{
    public Guid IdIdempotencia { get; set; } = default!;
    public string Cpf { get; set; } = default!;
    public string Senha { get; set; } = default!;

}
