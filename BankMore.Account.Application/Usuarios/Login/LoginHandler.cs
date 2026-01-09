using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using BankMore.JwtService.Security;
using BankMore.JwtService.Security.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace BankMore.Account.Application.Usuarios.Login;

public class LoginHandler : IAccountHandler<LoginCommand, ApiResult<object>>
{
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUsuarioRepository _userRepository;
    private readonly IContaCorrenteRepository _contaRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotenciaRepository _idempotenciaRepository;
    private readonly IIdempotencyService _idempotencyService;

    public LoginHandler(ITokenService tokenService, IRefreshTokenStore refreshTokenStore, IPasswordHasher passwordHasher, IUsuarioRepository userRepository,
        IContaCorrenteRepository contaRepository, IOptions<JwtSettings> jwtOptions, IUnitOfWork unitOfWork,
        IIdempotenciaRepository idempotenciaRepository, IIdempotencyService idempotencyService)
    {
        _tokenService = tokenService;
        _refreshTokenStore = refreshTokenStore;
        _passwordHasher = passwordHasher;
        _userRepository = userRepository;
        _contaRepository = contaRepository;
        _jwtSettings = jwtOptions.Value;
        _unitOfWork = unitOfWork;
        _idempotenciaRepository = idempotenciaRepository;
        _idempotencyService = idempotencyService;
    }
    public async Task<ApiResult<object>> Handle(LoginCommand request, CancellationToken ct)
    {
        var requisicao = $"Login|{request.Usuario}";

        (bool idempotenciaValida, ApiResult<object>? resultadoIdempotencia) = await _idempotencyService.ChecIdempotenciakAsync(request.IdIdempotencia, requisicao, ct);

        if (!idempotenciaValida)
            return resultadoIdempotencia!;

        var usuario = await ResolveUsuarioAsync(request.Usuario, ct);

        if (usuario is null)
            return ApiResult<object>.Fail(HttpStatusCode.Unauthorized, AccountErrors.UserUnauthorized);

        var senhaCorreta = _passwordHasher.VerificarSenha(request.Senha, usuario.Salt, usuario.Senha);

        if (!senhaCorreta)
            return ApiResult<object>.Fail(HttpStatusCode.Unauthorized, AccountErrors.UserUnauthorized);

        usuario.UltimoLogin = DateTime.Now;

        var idempotencia = new Idempotencia
        {
            ChaveIdempotencia = request.IdIdempotencia,
            Requisicao = requisicao,
            Resultado = "Ok"
        };

        var transactionResult = await _unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            await _idempotenciaRepository.CreateAsync(idempotencia, ct);
            await _userRepository.UpdateAsync(usuario, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }, ct);

        if (!transactionResult.Success)
            return ApiResult<object>.Fail(HttpStatusCode.InternalServerError, AccountErrors.InternalServerError, transactionResult.Message);

        var tokenResult = _tokenService.GenerateToken(usuario.ContaCorrente.IdContaCorrente);
        
        var refreshToken = new RefreshToken
        {
            Token = tokenResult.RefreshToken,
            UserId = usuario.IdUsuario,
            ExpiresAt = DateTime.Now.AddDays(_jwtSettings.RefreshTokenDays),
            Revoked = false
        };
        
        await _refreshTokenStore.SaveAsync(refreshToken, ct);
       
        var result = ApiResult<object>.Ok(tokenResult);
        
        idempotencia = await _idempotenciaRepository.GetAsync(request.IdIdempotencia, ct);
        idempotencia.Resultado = JsonSerializer.Serialize(result);
        _unitOfWork.SaveChangesAsync();

        return result;
    }

    private async Task<Usuario?> ResolveUsuarioAsync(string login, CancellationToken ct)
    {
        login = (login ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(login))
            return null;

        if (CpfValidator.Validate(login))
            return await _userRepository.GetByCpfAsync(login, ct);

        if (!int.TryParse(login, out var numeroConta))
            return null;

        var conta = await _contaRepository.GetByNumeroContaAsync(numeroConta, ct);
        if (conta is null)
            return null;

        return await _userRepository.GetByCpfAsync(conta.Usuario.Cpf, ct);
    }
}

