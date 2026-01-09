using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Application.Usuarios.CadastrarUsuario;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using Moq;
using System.Net;

namespace BankMore.Account.Tests.Usuarios.CadastrarUsuario;

public class CadastrarUsuarioHandlerTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock = new();
    private readonly Mock<IContaCorrenteRepository> _contaRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IIdempotenciaRepository> _idempotenciaRepoMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock = new();

    private readonly CadastrarUsuarioHandler _handler;

    private const string _cpfValido = "41083040049";
    private const string _senhaValida = "SenhaValida@123";

    public CadastrarUsuarioHandlerTests()
    {
        _handler = new CadastrarUsuarioHandler(
            _usuarioRepoMock.Object,
            _contaRepoMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _idempotenciaRepoMock.Object,
            _idempotencyServiceMock.Object);
    }

    private void SetupIdempotenciaOk(CadastrarUsuarioCommand command)
    {
        _idempotencyServiceMock
            .Setup(s => s.ChecIdempotenciakAsync(command.IdIdempotencia, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));
    }

    private void SetupIdempotenciaInvalida(CadastrarUsuarioCommand command, ApiResult<object> resultado)
    {
        _idempotencyServiceMock
            .Setup(s => s.ChecIdempotenciakAsync(command.IdIdempotencia, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, resultado));
    }

    private void SetupTransacaoOk()
    {
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (func, token) =>
            {
                await func(token);
                return TransactionResult.Ok();
            });
    }

    private void SetupTransacaoFalha(string message)
    {
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (func, token) =>
            {
                await func(token);
                return TransactionResult.Fail(message);
            });
    }

    private static CadastrarUsuarioCommand NovoCommand(string cpf, string senha)
        => new() { Nome = "Nome", Cpf = cpf, Senha = senha, IdIdempotencia = Guid.NewGuid() };

    [Fact]
    public async Task Handle_DeveRetornarResultadoIdempotencia_QuandoIdempotenciaInvalida()
    {
        var command = NovoCommand(_cpfValido, _senhaValida);
        var resultadoIdempotencia = ApiResult<object>.Fail(HttpStatusCode.Conflict, "IDEMPOTENCIA", "Já processado");

        SetupIdempotenciaInvalida(command, resultadoIdempotencia);

        var result = await _handler.Handle(command, default);

        Assert.Same(resultadoIdempotencia, result);

        _usuarioRepoMock.Verify(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _idempotenciaRepoMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Never);
        _contaRepoMock.Verify(c => c.CreateAsync(It.IsAny<ContaCorrente>(), It.IsAny<CancellationToken>()), Times.Never);
        _usuarioRepoMock.Verify(c => c.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarFail_QuandoUsuarioExistir()
    {
        var command = NovoCommand(_cpfValido, _senhaValida);

        SetupIdempotenciaOk(command);

        _usuarioRepoMock
            .Setup(r => r.GetByCpfAsync(command.Cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario());

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
        Assert.Equal(AccountErrors.Forbidden, result.Type);

        _usuarioRepoMock.Verify(r => r.GetByCpfAsync(command.Cpf, It.IsAny<CancellationToken>()), Times.Once);
        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarFail_QuandoCpfForInvalido()
    {
        var command = NovoCommand("invalid", _senhaValida);

        SetupIdempotenciaOk(command);

        _usuarioRepoMock
            .Setup(r => r.GetByCpfAsync(command.Cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidDocument, result.Type);

        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarFail_QuandoSenhaForInvalida()
    {
        var command = NovoCommand(_cpfValido, "123");

        SetupIdempotenciaOk(command);

        _usuarioRepoMock
            .Setup(r => r.GetByCpfAsync(command.Cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidValue, result.Type);

        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveCriarUsuarioEConta_QuandoSucesso()
    {
        var command = NovoCommand(_cpfValido, _senhaValida);

        SetupIdempotenciaOk(command);

        _usuarioRepoMock
            .Setup(r => r.GetByCpfAsync(command.Cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        _passwordHasherMock
            .Setup(h => h.HashPassword(command.Senha))
            .Returns(("hash", "salt"));

        SetupTransacaoOk();

        var result = await _handler.Handle(command, default);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.Status);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _idempotenciaRepoMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Once);
        _usuarioRepoMock.Verify(r => r.CreateAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Once);
        _contaRepoMock.Verify(r => r.CreateAsync(It.IsAny<ContaCorrente>(), It.IsAny<CancellationToken>()), Times.Once);

        _passwordHasherMock.Verify(h => h.HashPassword(command.Senha), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoTransacaoFalhar()
    {
        var command = NovoCommand(_cpfValido, _senhaValida);

        SetupIdempotenciaOk(command);

        _usuarioRepoMock
            .Setup(r => r.GetByCpfAsync(command.Cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        _passwordHasherMock
            .Setup(h => h.HashPassword(command.Senha))
            .Returns(("hash", "salt"));

        SetupTransacaoFalha("Erro de transação");

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.Status);
        Assert.Equal(AccountErrors.InternalServerError, result.Type);
        Assert.Equal("Erro de transação", result.Message);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
