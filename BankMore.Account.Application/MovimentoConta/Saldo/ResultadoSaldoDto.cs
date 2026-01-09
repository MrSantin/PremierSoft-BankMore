using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Application.MovimentoConta.Saldo;

public class ResultadoSaldoDto
{
    public int NumeroConta  { get; set; } = default!;
    public string NomeTitular { get; set; } = default!;
    public decimal Saldo { get; set; } = default!;
    public DateTime DataConsulta { get; set; } = DateTime.Now;

}

