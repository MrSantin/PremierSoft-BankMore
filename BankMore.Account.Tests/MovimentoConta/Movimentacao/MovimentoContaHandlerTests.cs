using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.MovimentoConta.Movimentacao;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using BankMore.Account.Domain.Repositories.Shared;
using Moq;
using System.Net;

namespace BankMore.Account.Tests.MovimentoConta.Movimentacao;

public class MovimentoContaHandlerTests
{
    private readonly Mock<IContaCorrenteRepository> _contaCorrenteRepoMock = new();
    private readonly Mock<IBankMoreAccountRepository<Movimento>> _movimentoRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IIdempotenciaRepository> _idempotenciaRepoMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock = new();

    private readonly MovimentoContaHandler _handler;

    public MovimentoContaHandlerTests()
    {
        _handler = new MovimentoContaHandler(
            _contaCorrenteRepoMock.Object,
            _movimentoRepoMock.Object,
            _idempotenciaRepoMock.Object,
            _unitOfWorkMock.Object,
            _idempotencyServiceMock.Object
        );
    }

    private void SetupIdempotencyOk()
    {
        _idempotencyServiceMock
            .Setup(x => x.ChecIdempotenciakAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));
    }

    private void SetupIdempotencyInvalid(ApiResult<object> result)
    {
        _idempotencyServiceMock
            .Setup(x => x.ChecIdempotenciakAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, result));
    }

    private void SetupTransactionOk()
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (func, ct) =>
            {
                await func(ct);
                return TransactionResult.Ok();
            });
    }

    private void SetupTransactionFail(string message = "Erro interno")
    {
        _unitOfWorkMock
            .Setup(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionResult.Fail(message));
    }


    [Fact]
    public async Task Handle_DeveRetornarResultadoIdempotencia_QuandoIdempotenciaInvalida()
    {
        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 10,
            ContaOrigem = Guid.NewGuid(),
            TipoMovimento = "C"
        };

        var idemResult = ApiResult<object>.Fail(HttpStatusCode.Conflict, AccountErrors.InvalidType, "Idempotência inválida");
        SetupIdempotencyInvalid(idemResult);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Conflict, result.Status);
        Assert.Equal(AccountErrors.InvalidType, result.Type);
        Assert.Equal("Idempotência inválida", result.Message);

        _contaCorrenteRepoMock.VerifyNoOtherCalls();
        _movimentoRepoMock.VerifyNoOtherCalls();
        _idempotenciaRepoMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoValorMenorOuIgualZero()
    {
        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 0,
            ContaOrigem = Guid.NewGuid(),
            TipoMovimento = "C"
        };

        SetupIdempotencyOk();

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidValue, result.Type);

        _unitOfWorkMock.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _movimentoRepoMock.Verify(x => x.CreateAsync(It.IsAny<Movimento>(), It.IsAny<CancellationToken>()), Times.Never);
        _idempotenciaRepoMock.Verify(x => x.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]

    [InlineData("X")]
    public async Task Handle_DeveRetornarBadRequest_QuandoTipoMovimentoInvalido(string? tipo)
    {
        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 10,
            ContaOrigem = Guid.NewGuid(),
            TipoMovimento = tipo
        };

        SetupIdempotencyOk();

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidType, result.Type);

        _unitOfWorkMock.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoContaDestinoNulaEContaOrigemNaoEncontrada()
    {
        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 10,
            ContaOrigem = Guid.NewGuid(),
            ContaDestino = null,
            TipoMovimento = "C"
        };

        SetupIdempotencyOk();

        _contaCorrenteRepoMock
            .Setup(x => x.GetAsync(cmd.ContaOrigem, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContaCorrente?)null);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidAccount, result.Type);

        _unitOfWorkMock.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoContaDestinoInformadaENaoEncontrada()
    {
        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 10,
            ContaOrigem = Guid.NewGuid(),
            ContaDestino = 12345,
            TipoMovimento = "C"
        };

        SetupIdempotencyOk();

        _contaCorrenteRepoMock
            .Setup(x => x.GetByNumeroContaAsync(cmd.ContaDestino.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContaCorrente?)null);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidAccount, result.Type);

        _unitOfWorkMock.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoContaInativa()
    {
        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 10,
            ContaOrigem = Guid.NewGuid(),
            ContaDestino = null,
            TipoMovimento = "C"
        };

        SetupIdempotencyOk();

        _contaCorrenteRepoMock
            .Setup(x => x.GetAsync(cmd.ContaOrigem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrente { IdContaCorrente = cmd.ContaOrigem, Ativo = false });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidAccount, result.Type);

        _unitOfWorkMock.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarNoContent_QuandoCreditoSimplesValido()
    {
        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 10,
            ContaOrigem = Guid.NewGuid(),
            ContaDestino = null,
            TipoMovimento = "C"
        };

        SetupIdempotencyOk();

        _contaCorrenteRepoMock
            .Setup(x => x.GetAsync(cmd.ContaOrigem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrente { IdContaCorrente = cmd.ContaOrigem, Ativo = true });

        SetupTransactionOk();

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.NoContent, result.Status);

        _unitOfWorkMock.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _idempotenciaRepoMock.Verify(x => x.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Once);
        _movimentoRepoMock.Verify(x => x.CreateAsync(It.IsAny<Movimento>(), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarOkComDto_QuandoTransferenciaValida()
    {
        var contaOrigem = Guid.NewGuid();

        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 50,
            ContaOrigem = contaOrigem,
            ContaDestino = 999,    
            TipoMovimento = "C"   
        };

        SetupIdempotencyOk();

        var contaDestinoEntity = new ContaCorrente
        {
            IdContaCorrente = Guid.NewGuid(), 
        };

        _contaCorrenteRepoMock
            .Setup(x => x.GetByNumeroContaAsync(cmd.ContaDestino.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contaDestinoEntity);

        SetupTransactionOk();

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.Status);

        Assert.NotNull(result.Data);
        var dto = Assert.IsType<ResultadoTransferenciaDto>(result.Data);
        Assert.Equal(contaDestinoEntity.IdContaCorrente, dto.IdContaDestino);

        _unitOfWorkMock.Verify(x => x.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
        _idempotenciaRepoMock.Verify(x => x.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Once);

        // Transferência = 2 movimentos
        _movimentoRepoMock.Verify(x => x.CreateAsync(It.IsAny<Movimento>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_DeveRetornarErro_QuandoTransacaoFalha()
    {
        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            Valor = 10,
            ContaOrigem = Guid.NewGuid(),
            ContaDestino = null,
            TipoMovimento = "C"
        };

        SetupIdempotencyOk();

        _contaCorrenteRepoMock
            .Setup(x => x.GetAsync(cmd.ContaOrigem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrente { IdContaCorrente = cmd.ContaOrigem, Ativo = true });

        SetupTransactionFail("Falha na transação");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.Status);
        Assert.Equal(AccountErrors.InternalServerError, result.Type);
        Assert.Equal("Falha na transação", result.Message);
    }

    [Fact]
    public async Task Handle_DeveRetornarNoContent_QuandoDebitoSimplesValido()
    {
        SetupIdempotencyOk();
        SetupTransactionOk();

        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            ContaOrigem = Guid.NewGuid(),
            ContaDestino = null,
            TipoMovimento = "D",
            Valor = 10
        };

        _contaCorrenteRepoMock
            .Setup(r => r.GetAsync(cmd.ContaOrigem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrente { IdContaCorrente = cmd.ContaOrigem, Ativo = true });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.NoContent, result.Status);

        _movimentoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Movimento>(), It.IsAny<CancellationToken>()), Times.Once);
        _idempotenciaRepoMock.Verify(r => r.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoDebitoComContaDestinoDiferente()
    {
        SetupIdempotencyOk();

        var cmd = new MovimentoContaCommand
        {
            IdIdempotencia = Guid.NewGuid(),
            ContaOrigem = Guid.NewGuid(),
            ContaDestino = 123,
            TipoMovimento = "D",
            Valor = 10
        };

        _contaCorrenteRepoMock
            .Setup(r => r.GetByNumeroContaAsync(cmd.ContaDestino.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContaCorrente { IdContaCorrente = Guid.NewGuid(), Ativo = true }); 

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidType, result.Type);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _movimentoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Movimento>(), It.IsAny<CancellationToken>()), Times.Never);
        _idempotenciaRepoMock.Verify(r => r.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

}
