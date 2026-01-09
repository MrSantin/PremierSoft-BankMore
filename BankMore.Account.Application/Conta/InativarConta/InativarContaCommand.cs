using BankMore.Account.Application.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BankMore.Account.Application.Conta.InativarConta;

public class InativarContaCommand : IAccountCommand
{
    public Guid IdIdempotencia { get; set; } = default!;
    public string Senha { get; set; } = default!;
    [JsonIgnore]
    public Guid IdConta { get; set; } = default!;
}

