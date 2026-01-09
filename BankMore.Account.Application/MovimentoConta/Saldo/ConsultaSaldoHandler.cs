using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Repositories;

namespace BankMore.Account.Application.MovimentoConta.Saldo;

public class ConsultaSaldoHandler : IAccountHandler<ConsultaSaldoCommand, ApiResult<object>>
{
    private readonly IContaCorrenteRepository _contaRepository;
    private readonly IMovimentoRepository _repository;
    public ConsultaSaldoHandler(IContaCorrenteRepository contaRepository, IMovimentoRepository repository)
    {
        _contaRepository = contaRepository;
        _repository = repository;
    }
    public async Task<ApiResult<object>> Handle(ConsultaSaldoCommand request, CancellationToken ct)
    {
        var conta = await _contaRepository.GetAsync(request.IdConta, ct);
        if (conta is null)
            return ApiResult<object>.Fail(System.Net.HttpStatusCode.NotFound, AccountErrors.InvalidAccount, "A conta informada não foi encontrada");
        
        if(!conta.Ativo)
            return ApiResult<object>.Fail(System.Net.HttpStatusCode.NotFound, AccountErrors.InvalidAccount, "A conta informada está desativada");

        var saldo = await _repository.GetSaldoAsync(conta.IdContaCorrente, ct);

        var result = new ResultadoSaldoDto
        {
            NumeroConta = conta.Numero,
            NomeTitular = conta.Nome,
            Saldo = saldo
        };

        return ApiResult<object>.Ok(result);
    }
}

