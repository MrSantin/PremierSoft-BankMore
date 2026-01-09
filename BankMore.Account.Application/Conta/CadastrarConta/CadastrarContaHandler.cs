using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Application.Usuarios;
using BankMore.Account.Application.Usuarios.CadastrarUsuario;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using MediatR;
using System.Net;
using System.Text.Json;

namespace BankMore.Account.Application.Conta.CadastrarConta;

public class CadastrarContaHandler : IAccountHandler<CadastrarContaCommand, ApiResult<object>>
{
    private readonly IContaCorrenteRepository _repository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdempotenciaRepository _idempotenciaRepository;
    private readonly IIdempotencyService _idempotencyService;
    public CadastrarContaHandler(IContaCorrenteRepository repository, IUsuarioRepository usuarioRepository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher,
                                 IIdempotenciaRepository idempotenciaRepository, IIdempotencyService idempotencyService)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _idempotenciaRepository = idempotenciaRepository;
        _idempotencyService = idempotencyService;
    }
    public async Task<ApiResult<object>> Handle(CadastrarContaCommand request, CancellationToken ct)
    {
        //Como a conta é incluída no registro do usuário como inativa, aqui apenas o status para ativo, o salt e a senha da conta
        //Assumi que essa seria a melhor forma, uma vez que é necessário retornar as informações da conta no momento do login
        var requisicao = $"CadastroConta|{request.Cpf}";

        (bool idempotenciaValida, ApiResult<object>? resultadoIdempotencia) = await _idempotencyService.ChecIdempotenciakAsync(request.IdIdempotencia, requisicao, ct);

        if (!idempotenciaValida)
            return resultadoIdempotencia!;

        if (!CpfValidator.Validate(request.Cpf))
            return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidDocument);

        var usuario = await _usuarioRepository.GetByCpfAsync(request.Cpf, ct);

        if (usuario is null)
            return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidValue);

        if (!PasswordValidator.SenhaValida(request.Senha))
            return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidValue);

        var conta = await _repository.GetAsync(usuario.ContaCorrente.IdContaCorrente, ct);

        var senha = _passwordHasher.HashPassword(request.Senha);

        conta.Salt = senha.Salt;
        conta.Senha = senha.Hash;
        conta.Ativo = true;

        var result = ApiResult<object>.Ok();

        var idempotencia = new Idempotencia
        {
            ChaveIdempotencia = request.IdIdempotencia,
            Requisicao = requisicao,
            Resultado = JsonSerializer.Serialize(result)
        };

        var transactionResult = await _unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            await _idempotenciaRepository.CreateAsync(idempotencia, transactionCt);
        }, ct);

        if (!transactionResult.Success)
            return ApiResult<object>.Fail(HttpStatusCode.InternalServerError, AccountErrors.InternalServerError, transactionResult.Message);

        return result;
    }
}
