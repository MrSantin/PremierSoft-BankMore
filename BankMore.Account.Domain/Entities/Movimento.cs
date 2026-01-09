using BankMore.Account.Domain.Entities.Shared;

namespace BankMore.Account.Domain.Entities;

public class Movimento : IEntity
{
    public Guid IdMovimento { get; set; } = Guid.NewGuid(); //Optei por utilizar Guid, pois possui menos de 37 caracteres e é melhor e mais performático de utilizar que string   
    public Guid IdContaCorrente { get; set; } = default!;
    public DateTime DataMovimento { get; set; }
    public TipoMovimento TipoMovimento { get; set; }
    public decimal Valor { get; set; }
    public ContaCorrente ContaCorrente { get; set; } = default!;
}
public enum TipoMovimento
{
    Credito = 'C',
    Debito = 'D'
}

