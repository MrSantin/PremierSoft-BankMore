using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using MediatR;
using System;
using System.Net;
using System.Text.Json;

namespace BankMore.Account.Application.Usuarios.CadastrarUsuario;

public class CadastrarUsuarioHandler : IAccountHandler<CadastrarUsuarioCommand, ApiResult<object>>
{
    private readonly IUsuarioRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IContaCorrenteRepository _contaRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotenciaRepository _idempotenciaRepository;
    private readonly IIdempotencyService _idempotencyService;
    public CadastrarUsuarioHandler(IUsuarioRepository repository, IContaCorrenteRepository contaRepository, IUnitOfWork unitOfWork,
                                   IPasswordHasher passwordHasher, IIdempotenciaRepository idempotenciaRepository, IIdempotencyService idempotencyService)
    {
        _repository = repository;
        _contaRepository = contaRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _idempotenciaRepository = idempotenciaRepository;
        _idempotencyService = idempotencyService;
    }
    public async Task<ApiResult<object>> Handle(CadastrarUsuarioCommand request, CancellationToken ct)
    {
        //Assumi que seria mais seguro ter um cadastro de usuario diferente do cadastro da conta, para evitar de utilizar o mesmo usuario e senha para 
        //fazer login e movimentar a conta. Por isso criei a entidade ContaCorrente separada da entidade Usuario.
        //Sei que criar a conta nesse handler quebra um pouco o princípio da Single Responsability, mas como é necessário retornar a informação da conta
        //no login, foi a melhor forma que encontrei para garantir que o usuario e a conta sejam criados juntos e de forma idempotente.

        var requisicao = $"CadastroUsuario|{request.Nome}|{request.Cpf}";

        (bool idempotenciaValida, ApiResult<object>? resultadoIdempotencia) = await _idempotencyService.ChecIdempotenciakAsync(request.IdIdempotencia, requisicao, ct);

        if (!idempotenciaValida)
            return resultadoIdempotencia!;

        var userExists = await _repository.GetByCpfAsync(request.Cpf, ct);
        if (userExists != null)
            return ApiResult<object>.Fail(HttpStatusCode.Forbidden, AccountErrors.Forbidden, "Usuário já existe");

        if (!CpfValidator.Validate(request.Cpf))
            return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidDocument);

        if (!PasswordValidator.SenhaValida(request.Senha))
            return ApiResult<object>.Fail(HttpStatusCode.BadRequest, AccountErrors.InvalidValue);

        var password = _passwordHasher.HashPassword(request.Senha);
        var usuario = new Usuario
        {
            Nome = request.Nome,
            Cpf = request.Cpf,
            Senha = password.Hash,
            Salt = password.Salt,
            UltimoLogin = DateTime.MinValue
        };

        var result = ApiResult<object>.Ok();

        var idempotencia = new Idempotencia
        {
            ChaveIdempotencia = request.IdIdempotencia,
            Requisicao = requisicao,
            Resultado = JsonSerializer.Serialize(result)
        };

        var passConta = _passwordHasher.HashPassword(request.Senha);

        var conta = new ContaCorrente
        {
            IdUsuario = usuario.IdUsuario,
            Nome = usuario.Nome,
            Senha = passConta.Hash,
            Salt = passConta.Salt,
            Ativo = false
        };

        var transactionResult = await _unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            await _idempotenciaRepository.CreateAsync(idempotencia, transactionCt);
            await _repository.CreateAsync(usuario, transactionCt);
            await _contaRepository.CreateAsync(conta, transactionCt);
        }, ct);

        if (!transactionResult.Success)
            return ApiResult<object>.Fail(HttpStatusCode.InternalServerError, AccountErrors.InternalServerError, transactionResult.Message);

        return result;
    }
}

