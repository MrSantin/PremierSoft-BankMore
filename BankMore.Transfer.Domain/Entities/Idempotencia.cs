using BankMore.Transfer.Domain.Entities.Shared;

namespace BankMore.Transfer.Domain.Entities;

public class Idempotencia : IEntity
{
    public Guid ChaveIdempotencia { get; set; } = default!;
    public string Requisicao { get; set; } = default!;
    public string Resultado { get; set; } = default!;
}

