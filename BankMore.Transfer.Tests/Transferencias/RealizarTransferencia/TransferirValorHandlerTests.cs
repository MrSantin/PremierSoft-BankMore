using BankMore.Transfer.Application.Abstractions;
using BankMore.Transfer.Application.Clients.Accounts.Api;
using BankMore.Transfer.Application.Services.Idempotencia;
using BankMore.Transfer.Application.Shared;
using BankMore.Transfer.Application.Transferencias.RealizarTransferencia;
using BankMore.Transfer.Domain.Entities;
using BankMore.Transfer.Domain.Repositories;
using Moq;
using System.Net;
using System.Text.Json;

namespace BankMore.Transfer.Tests.Transferencias.RealizarTransferencia;

public class TransferirValorHandlerTests
{
    private readonly Mock<ITransferenciaRepository> _repositoryMock = new();
    private readonly Mock<IIdempotenciaRepository> _idempotenciaRepositoryMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock = new();
    private readonly Mock<IAccountApiClient> _accountApiMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private readonly TransferirValorHandler _handler;

    public TransferirValorHandlerTests()
    {
        _handler = new TransferirValorHandler(
            _repositoryMock.Object,
            _idempotenciaRepositoryMock.Object,
            _idempotencyServiceMock.Object,
            _accountApiMock.Object,
            _unitOfWorkMock.Object);
    }

    private static MovimentoContaCommand NovoCommand(decimal valor = 100)
        => new()
        {
            ContaOrigem = Guid.NewGuid(),
            ContaDestino = 12345,
            Valor = valor,
            IdIdempotencia = Guid.NewGuid()
        };

    private void SetupIdempotenciaOk()
    {
        _idempotencyServiceMock
            .Setup(s => s.ChecIdempotenciakAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));
    }

    private void SetupIdempotenciaInvalida(ApiResult<object> resultado)
    {
        _idempotencyServiceMock
            .Setup(s => s.ChecIdempotenciakAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, resultado));
    }

    private void SetupTransacaoOk()
    {
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (func, ct) =>
            {
                await func(ct);
                return TransactionResult.Ok();
            });
    }

    private void SetupTransacaoFalha(string message)
    {
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionResult.Fail(message));
    }

    private static ResultadoTransferenciaDto NovoResultadoTransferencia()
        => new()
        {
            IdContaDestino = Guid.NewGuid(),
            DataMovimento = DateTime.Now
        };

    [Fact]
    public async Task Handle_DeveRetornarResultadoIdempotencia_QuandoIdempotenciaInvalida()
    {
        var command = NovoCommand();
        var esperado = ApiResult<object>.Fail(HttpStatusCode.Conflict, "IDEMPOTENCIA");

        SetupIdempotenciaInvalida(esperado);

        var result = await _handler.Handle(command, default);

        Assert.Same(esperado, result);

        _accountApiMock.Verify(a => a.MovimentarContaAsync(It.IsAny<MovimentoContaCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarForbidden_QuandoValorInvalido()
    {
        var command = NovoCommand(0);

        SetupIdempotenciaOk();

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
        Assert.Equal(TransferErrors.InvalidValue, result.Type);

        _accountApiMock.Verify(a => a.MovimentarContaAsync(It.IsAny<MovimentoContaCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoMovimentacaoNaApiFalhar()
    {
        var command = NovoCommand();

        SetupIdempotenciaOk();

        _accountApiMock
            .Setup(a => a.MovimentarContaAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<object>.Fail(HttpStatusCode.BadRequest, "ErroConta"));

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal("ErroConta", result.Type);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarInternalServerError_QuandoTransacaoFalhar()
    {
        var command = NovoCommand();
        var resultadoTransferencia = NovoResultadoTransferencia();

        SetupIdempotenciaOk();

        _accountApiMock
            .Setup(a => a.MovimentarContaAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<object>.Ok(JsonSerializer.Serialize(resultadoTransferencia)));

        SetupTransacaoFalha("Erro transação");

        var result = await _handler.Handle(command, default);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.Status);
        Assert.Equal(TransferErrors.InternalServerError, result.Type);
        Assert.Equal("Erro transação", result.Message);

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Transferencia>(), It.IsAny<CancellationToken>()), Times.Never);
        _idempotenciaRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DevePersistirTransferenciaEIdempotencia_QuandoSucesso()
    {
        var command = NovoCommand();
        var resultadoTransferencia = NovoResultadoTransferencia();

        SetupIdempotenciaOk();
        SetupTransacaoOk();

        _accountApiMock
            .Setup(a => a.MovimentarContaAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<object>.Ok(JsonSerializer.Serialize(resultadoTransferencia)));

        var result = await _handler.Handle(command, default);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.NoContent, result.Status);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);

        _repositoryMock.Verify(r =>
            r.CreateAsync(It.Is<Transferencia>(t =>
                t.IdContaOrigem == command.ContaOrigem &&
                t.IdContaDestino == resultadoTransferencia.IdContaDestino &&
                t.Valor == command.Valor
            ), It.IsAny<CancellationToken>()), Times.Once);

        _idempotenciaRepositoryMock.Verify(r =>
            r.CreateAsync(It.Is<Idempotencia>(i =>
                i.ChaveIdempotencia == command.IdIdempotencia &&
                !string.IsNullOrWhiteSpace(i.Resultado)
            ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
