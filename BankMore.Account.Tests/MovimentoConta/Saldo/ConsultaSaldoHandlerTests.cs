using System.Net;
using BankMore.Account.Application.MovimentoConta.Saldo;
using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using Moq;
using Xunit;

namespace BankMore.Account.Tests.MovimentoConta.Saldo;
public class ConsultaSaldoHandlerTests
{
    private readonly Mock<IContaCorrenteRepository> _contaRepoMock;
    private readonly Mock<IMovimentoRepository> _movimentoRepoMock;
    private readonly ConsultaSaldoHandler _handler;

    public ConsultaSaldoHandlerTests()
    {
        _contaRepoMock = new Mock<IContaCorrenteRepository>();
        _movimentoRepoMock = new Mock<IMovimentoRepository>();
        _handler = new ConsultaSaldoHandler(_contaRepoMock.Object, _movimentoRepoMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarFail_QuandoContaNaoEncontrada()
    {
        // Arrange
        var command = new ConsultaSaldoCommand { IdConta = Guid.NewGuid() };
        _contaRepoMock.Setup(r => r.GetAsync(command.IdConta, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContaCorrente)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.Status);
        Assert.Equal(AccountErrors.InvalidAccount, result.Type);
        Assert.Equal("A conta informada não foi encontrada", result.Message);
    }

    [Fact]
    public async Task Handle_DeveRetornarFail_QuandoContaInativa()
    {
        // Arrange
        var conta = new ContaCorrente
        {
            IdContaCorrente = Guid.NewGuid(),
            Numero = 12345,
            Nome = "Teste",
            Ativo = false
        };
        var command = new ConsultaSaldoCommand { IdConta = conta.IdContaCorrente };
        _contaRepoMock.Setup(r => r.GetAsync(command.IdConta, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.Status);
        Assert.Equal(AccountErrors.InvalidAccount, result.Type);
        Assert.Equal("A conta informada está desativada", result.Message);
    }

    [Fact]
    public async Task Handle_DeveRetornarOk_QuandoContaValida()
    {
        // Arrange
        var conta = new ContaCorrente
        {
            IdContaCorrente = Guid.NewGuid(),
            Numero = 54321,
            Nome = "Titular",
            Ativo = true
        };
        var command = new ConsultaSaldoCommand { IdConta = conta.IdContaCorrente };
        _contaRepoMock.Setup(r => r.GetAsync(command.IdConta, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);
        _movimentoRepoMock.Setup(r => r.GetSaldoAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1500.75m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        var saldoDto = Assert.IsType<ResultadoSaldoDto>(result.Data);
        Assert.Equal(conta.Numero, saldoDto.NumeroConta);
        Assert.Equal(conta.Nome, saldoDto.NomeTitular);
        Assert.Equal(1500.75m, saldoDto.Saldo);
    }
}
