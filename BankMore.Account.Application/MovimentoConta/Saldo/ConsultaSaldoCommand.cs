using BankMore.Account.Application.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Application.MovimentoConta.Saldo;

public class ConsultaSaldoCommand : IAccountCommand
{
    public Guid IdIdempotencia { get; set; } = default!;
    public Guid IdConta { get; set; } = default!;
}

