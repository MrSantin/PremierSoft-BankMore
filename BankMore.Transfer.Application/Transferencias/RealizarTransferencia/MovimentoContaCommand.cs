using BankMore.Transfer.Application.Shared;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BankMore.Transfer.Application.Transferencias.RealizarTransferencia;

public class MovimentoContaCommand : ITransferCommand
{

    public decimal Valor { get; set; } = default!;
    public string TipoMovimento { get; set; } = "C";
    [JsonIgnore]
    public Guid ContaOrigem { get; set; } = default!;
    public int ContaDestino { get; set; } = default!;
    public Guid IdIdempotencia { get; set; } = default!;

}
