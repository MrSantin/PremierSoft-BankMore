using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BankMore.Transfer.Application.Transferencias.RealizarTransferencia;

public sealed class ResultadoTransferenciaDto
{
    public Guid IdContaDestino { get; set; } = default!;
    public DateTime DataMovimento { get; set; } = default!;
}
