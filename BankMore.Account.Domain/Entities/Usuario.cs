using BankMore.Account.Domain.Entities.Shared;

namespace BankMore.Account.Domain.Entities;
//Precisei criar uma classe e tabela de usuario, não há na especificação, porém é importante segregar o usuário da conta corrente
//Haverá uma senha para login no sistema e outra para movimentação da conta
public class Usuario : IEntity
{
    public Guid IdUsuario { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = default!;
    public string Cpf { get; set; } = default!;
    public string Senha { get; set; } = default!;
    public string Salt { get; set; } = default!;
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime UltimoLogin { get; set; } = default!;
    public ContaCorrente? ContaCorrente { get; set; } = default!;
}

