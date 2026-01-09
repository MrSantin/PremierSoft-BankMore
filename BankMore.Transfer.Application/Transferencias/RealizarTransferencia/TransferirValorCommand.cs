using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Transfer.Application.Transferencias.RealizarTransferencia;


public class TransferirValorCommand
{
    public decimal Valor { get; set; } = default!;
    public int ContaDestino { get; set; } = default!;
    public Guid IdIdempotencia { get; set; } = default!;
}
