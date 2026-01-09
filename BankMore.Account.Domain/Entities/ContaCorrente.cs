using BankMore.Account.Domain.Entities.Shared;

namespace BankMore.Account.Domain.Entities;

public class ContaCorrente : IEntity
{
    public Guid IdContaCorrente { get; set; } = Guid.NewGuid(); //Optei por utilizar Guid, pois possui menos de 37 caracteres e é melhor e mais performático de utilizar que string   
    public int Numero { get; set; }
    public string Nome { get; set; } = default!;
    public bool Ativo { get; set; } = true;
    public string Senha { get; set; } = default!;
    public string Salt { get; set; } = default!;
    public Guid IdUsuario { get; set; } = default!;
    public Usuario Usuario { get; set; } = default!;
}

