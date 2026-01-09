using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Application.Usuarios.Login;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using BankMore.JwtService.Security;
using BankMore.JwtService.Security.Models;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;

namespace BankMore.Account.Tests.Usuarios.Login;

public class LoginHandlerTests
{
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IRefreshTokenStore> _refreshTokenStoreMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUsuarioRepository> _userRepositoryMock = new();
    private readonly Mock<IContaCorrenteRepository> _contaRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdempotenciaRepository> _idempotenciaRepositoryMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock = new();

    private readonly JwtSettings _jwtSettings = new() { RefreshTokenDays = 7 };
    private readonly IOptions<JwtSettings> _jwtOptions;

    private readonly LoginHandler _handler;

    private const string _cpfValido = "41083040049";

    public LoginHandlerTests()
    {
        _jwtOptions = Options.Create(_jwtSettings);

        _handler = new LoginHandler(
            _tokenServiceMock.Object,
            _refreshTokenStoreMock.Object,
            _passwordHasherMock.Object,
            _userRepositoryMock.Object,
            _contaRepositoryMock.Object,
            _jwtOptions,
            _unitOfWorkMock.Object,
            _idempotenciaRepositoryMock.Object,
            _idempotencyServiceMock.Object);
    }

    private void SetupIdempotenciaOk()
    {
        _idempotencyServiceMock
            .Setup(x => x.ChecIdempotenciakAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));
    }

    private void SetupIdempotenciaInvalida(ApiResult<object> resultado)
    {
        _idempotencyServiceMock
            .Setup(x => x.ChecIdempotenciakAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, resultado));
    }

    private void SetupTransacaoOk()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (func, ct) =>
            {
                await func(ct);
                return TransactionResult.Ok();
            });
    }

    private void SetupTransacaoFalha(string message)
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionResult.Fail(message));
    }

    private static Usuario NovoUsuario()
    {
        var conta = new ContaCorrente { IdContaCorrente = Guid.NewGuid() };

        return new Usuario
        {
            IdUsuario = Guid.NewGuid(),
            Salt = "salt",
            Senha = "hash",
            ContaCorrente = conta,
            UltimoLogin = DateTime.MinValue
        };
    }

    [Fact]
    public async Task Handle_DeveRetornarResultadoIdempotencia_QuandoIdempotenciaInvalida()
    {
        var resultadoIdempotencia = ApiResult<object>.Fail(HttpStatusCode.Conflict, "IDEMPOTENCIA", "Já processado");
        var command = new LoginCommand { Usuario = _cpfValido, Senha = "senha", IdIdempotencia = Guid.NewGuid() };

        SetupIdempotenciaInvalida(resultadoIdempotencia);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Same(resultadoIdempotencia, result);

        _userRepositoryMock.Verify(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasherMock.Verify(h => h.VerificarSenha(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<Guid>()), Times.Never);
        _refreshTokenStoreMock.Verify(s => s.SaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarUnauthorized_QuandoUsuarioNaoEncontrado()
    {
        var command = new LoginCommand { Usuario = _cpfValido, Senha = "senha", IdIdempotencia = Guid.NewGuid() };

        SetupIdempotenciaOk();

        _userRepositoryMock
            .Setup(r => r.GetByCpfAsync(_cpfValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Equal(AccountErrors.UserUnauthorized, result.Type);

        _passwordHasherMock.Verify(h => h.VerificarSenha(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarUnauthorized_QuandoSenhaIncorreta()
    {
        var usuario = NovoUsuario();
        var command = new LoginCommand { Usuario = _cpfValido, Senha = "senhaErrada", IdIdempotencia = Guid.NewGuid() };

        SetupIdempotenciaOk();

        _userRepositoryMock
            .Setup(r => r.GetByCpfAsync(_cpfValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _passwordHasherMock
            .Setup(h => h.VerificarSenha(command.Senha, usuario.Salt, usuario.Senha))
            .Returns(false);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Equal(AccountErrors.UserUnauthorized, result.Type);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<Guid>()), Times.Never);
        _refreshTokenStoreMock.Verify(s => s.SaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarErroInterno_QuandoTransacaoFalhar()
    {
        var usuario = NovoUsuario();
        var command = new LoginCommand { Usuario = _cpfValido, Senha = "senha", IdIdempotencia = Guid.NewGuid() };

        SetupIdempotenciaOk();

        _userRepositoryMock
            .Setup(r => r.GetByCpfAsync(_cpfValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _passwordHasherMock
            .Setup(h => h.VerificarSenha(command.Senha, usuario.Salt, usuario.Senha))
            .Returns(true);

        SetupTransacaoFalha("Erro na transação");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.Status);
        Assert.Equal(AccountErrors.InternalServerError, result.Type);
        Assert.Equal("Erro na transação", result.Message);

        _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<Guid>()), Times.Never);
        _refreshTokenStoreMock.Verify(s => s.SaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarSucesso_QuandoLoginPorCpfValido()
    {
        var usuario = NovoUsuario();
        var tokenResult = new TokenResult { AccessToken = "access", RefreshToken = "refresh" };
        var command = new LoginCommand { Usuario = _cpfValido, Senha = "senha", IdIdempotencia = Guid.NewGuid() };

        SetupIdempotenciaOk();
        SetupTransacaoOk();

        _userRepositoryMock
            .Setup(r => r.GetByCpfAsync(_cpfValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _passwordHasherMock
            .Setup(h => h.VerificarSenha(command.Senha, usuario.Salt, usuario.Senha))
            .Returns(true);

        _tokenServiceMock
            .Setup(t => t.GenerateToken(usuario.ContaCorrente.IdContaCorrente))
            .Returns(tokenResult);

        _refreshTokenStoreMock
            .Setup(s => s.SaveAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _idempotenciaRepositoryMock
            .Setup(r => r.GetAsync(command.IdIdempotencia, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Idempotencia());

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.Same(tokenResult, result.Data);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _idempotenciaRepositoryMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        _tokenServiceMock.Verify(t => t.GenerateToken(usuario.ContaCorrente.IdContaCorrente), Times.Once);
        _refreshTokenStoreMock.Verify(s => s.SaveAsync(It.Is<RefreshToken>(x =>
            x.Token == tokenResult.RefreshToken &&
            x.UserId == usuario.IdUsuario &&
            x.Revoked == false
        ), It.IsAny<CancellationToken>()), Times.Once);

        _idempotenciaRepositoryMock.Verify(r => r.GetAsync(command.IdIdempotencia, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarUnauthorized_QuandoLoginForVazio()
    {
        var command = new LoginCommand { Usuario = "   ", Senha = "senha", IdIdempotencia = Guid.NewGuid() };

        SetupIdempotenciaOk();

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Equal(AccountErrors.UserUnauthorized, result.Type);

        _userRepositoryMock.Verify(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _contaRepositoryMock.Verify(r => r.GetByNumeroContaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarUnauthorized_QuandoLoginPorNumeroContaNaoEncontrarConta()
    {
        var command = new LoginCommand { Usuario = "12345", Senha = "senha", IdIdempotencia = Guid.NewGuid() };

        SetupIdempotenciaOk();

        _contaRepositoryMock
            .Setup(r => r.GetByNumeroContaAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContaCorrente?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Equal(AccountErrors.UserUnauthorized, result.Type);

        _userRepositoryMock.Verify(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
