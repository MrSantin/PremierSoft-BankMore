using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Application.Usuarios.Login;

public class LoginCommand : IAccountCommand
{
    public Guid IdIdempotencia { get; set; } = default!;
    public string Usuario { get; set; } = default!;
    public string Senha { get; set; } = default!;
}

