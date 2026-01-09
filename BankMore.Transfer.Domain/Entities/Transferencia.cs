using BankMore.Transfer.Domain.Entities.Shared;

namespace BankMore.Transfer.Domain.Entities;

public class Transferencia : IEntity
{
    public Guid IdTransferencia { get; set; } = Guid.NewGuid();
    public Guid IdContaOrigem { get; set; } = default!;
    public Guid IdContaDestino { get; set; } = default!;
    public DateTime DataMovimento { get; set; } = default!;
    public decimal Valor { get; set; } = default!;
}
