using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Application.MovimentoConta.Movimentacao;

public sealed class ResultadoTransferenciaDto
{
    public Guid IdContaDestino { get; set; } = default!;
    public DateTime DataMovimento { get; set; } = default!;
}
