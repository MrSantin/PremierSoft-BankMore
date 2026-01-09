using BankMore.Account.Domain.Entities.Shared;

namespace BankMore.Account.Domain.Entities;

public class Idempotencia : IEntity
{
    public Guid ChaveIdempotencia { get; set; } = default!;
    public string Requisicao { get; set; } = default!;
    public string Resultado { get; set; } = default!;
}

