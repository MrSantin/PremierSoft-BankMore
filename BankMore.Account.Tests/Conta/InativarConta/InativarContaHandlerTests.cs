using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Conta.InativarConta;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using Moq;
using System.Net;

namespace BankMore.Account.Tests.Conta.InativarConta;

public class InativarContaHandlerTests
{
    private readonly Mock<IContaCorrenteRepository> _repositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdempotenciaRepository> _idempotenciaRepositoryMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock = new();

    private readonly InativarContaHandler _handler;

    public InativarContaHandlerTests()
    {
        _handler = new InativarContaHandler(
            _repositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _idempotenciaRepositoryMock.Object,
            _idempotencyServiceMock.Object);
    }

    private void SetupIdempotenciaOk(InativarContaCommand command)
    {
        _idempotencyServiceMock
            .Setup(s => s.ChecIdempotenciakAsync(command.IdIdempotencia, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));
    }

    private void SetupIdempotenciaInvalida(InativarContaCommand command, ApiResult<object> resultado)
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

    private static InativarContaCommand NovoCommand()
        => new() { IdConta = Guid.NewGuid(), Senha = "senha", IdIdempotencia = Guid.NewGuid() };

    private static ContaCorrente NovaConta(Guid idConta, bool ativo)
        => new() { IdContaCorrente = idConta, Ativo = ativo, Salt = "salt", Senha = "hash" };

    [Fact]
    public async Task Handle_DeveRetornarUnauthorized_QuandoSenhaIncorreta()
    {
        var command = NovoCommand();
        var conta = NovaConta(command.IdConta, ativo: true);

        _repositoryMock.Setup(r => r.GetAsync(command.IdConta, It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasherMock.Setup(h => h.VerificarSenha(command.Senha, conta.Salt, conta.Senha)).Returns(false);

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.Status);
        Assert.Equal(AccountErrors.UserUnauthorized, result.Type);

        _idempotencyServiceMock.Verify(s => s.ChecIdempotenciakAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _idempotenciaRepositoryMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoContaJaInativa()
    {
        var command = NovoCommand();
        var conta = NovaConta(command.IdConta, ativo: false);

        _repositoryMock.Setup(r => r.GetAsync(command.IdConta, It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasherMock.Setup(h => h.VerificarSenha(command.Senha, conta.Salt, conta.Senha)).Returns(true);

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidAccount, result.Type);
        Assert.Equal("A conta já está inativada", result.Message);

        _idempotencyServiceMock.Verify(s => s.ChecIdempotenciakAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _idempotenciaRepositoryMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarResultadoIdempotencia_QuandoIdempotenciaInvalida()
    {
        var command = NovoCommand();
        var conta = NovaConta(command.IdConta, ativo: true);
        var resultadoIdempotencia = ApiResult<object>.Fail(HttpStatusCode.Conflict, "IDEMPOTENCIA", "Já processado");

        _repositoryMock.Setup(r => r.GetAsync(command.IdConta, It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasherMock.Setup(h => h.VerificarSenha(command.Senha, conta.Salt, conta.Senha)).Returns(true);
        SetupIdempotenciaInvalida(command, resultadoIdempotencia);

        var result = await _handler.Handle(command, default);

        Assert.Same(resultadoIdempotencia, result);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _idempotenciaRepositoryMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarInternalServerError_QuandoTransacaoFalha()
    {
        var command = NovoCommand();
        var conta = NovaConta(command.IdConta, ativo: true);

        _repositoryMock.Setup(r => r.GetAsync(command.IdConta, It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasherMock.Setup(h => h.VerificarSenha(command.Senha, conta.Salt, conta.Senha)).Returns(true);
        SetupIdempotenciaOk(command);
        SetupTransacaoFalha("Erro de transação");

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.Status);
        Assert.Equal(AccountErrors.InternalServerError, result.Type);
        Assert.Equal("Erro de transação", result.Message);

        _idempotenciaRepositoryMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarNoContent_QuandoSucesso()
    {
        var command = NovoCommand();
        var conta = NovaConta(command.IdConta, ativo: true);

        _repositoryMock.Setup(r => r.GetAsync(command.IdConta, It.IsAny<CancellationToken>())).ReturnsAsync(conta);
        _passwordHasherMock.Setup(h => h.VerificarSenha(command.Senha, conta.Salt, conta.Senha)).Returns(true);
        SetupIdempotenciaOk(command);
        SetupTransacaoOk();

        var result = await _handler.Handle(command, default);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.NoContent, result.Status);
        Assert.False(conta.Ativo);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _idempotenciaRepositoryMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
