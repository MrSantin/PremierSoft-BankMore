using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using System.Net;
using System.Text.Json;

namespace BankMore.Account.Application.Conta.InativarConta;

public class InativarContaHandler : IAccountHandler<InativarContaCommand, ApiResult<object>>
{
    private readonly IContaCorrenteRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotenciaRepository _idempotenciaRepository;
    private readonly IIdempotencyService _idempotencyService;
    public InativarContaHandler(IContaCorrenteRepository repository, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork,
                                IIdempotenciaRepository idempotenciaRepository, IIdempotencyService idempotencyService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _idempotenciaRepository = idempotenciaRepository;
        _idempotencyService = idempotencyService;
    }
    public async Task<ApiResult<object>> Handle(InativarContaCommand request, CancellationToken ct)
    {
        var conta = await _repository.GetAsync(request.IdConta, ct);
        var senhaCorreta = _passwordHasher.VerificarSenha(request.Senha, conta.Salt, conta.Senha);
        if (!senhaCorreta)
            return ApiResult<object>.Fail(HttpStatusCode.Unauthorized, AccountErrors.UserUnauthorized);

        if (!conta.Ativo)
            return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidAccount, "A conta já está inativada");

        var requisicao = $"{request.IdConta}|InativarConta";

        (bool idempotenciaValida, ApiResult<object>? resultadoIdempotencia) = await _idempotencyService.ChecIdempotenciakAsync(request.IdIdempotencia, requisicao, ct);

        if (!idempotenciaValida)
            return resultadoIdempotencia!;

        var result = ApiResult<object>.NoContent();

        var idempotencia = new Idempotencia
        {
            ChaveIdempotencia = request.IdIdempotencia,
            Requisicao = requisicao,
            Resultado = JsonSerializer.Serialize(ApiResult<object>.NoContent())
        };

        conta.Ativo = false;

        var transactionResult = await _unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            await _idempotenciaRepository.CreateAsync(idempotencia, transactionCt);
        }, ct);

        if (!transactionResult.Success)
            return ApiResult<object>.Fail(HttpStatusCode.InternalServerError, AccountErrors.InternalServerError, transactionResult.Message);

        return ApiResult<object>.NoContent();
    }
}

