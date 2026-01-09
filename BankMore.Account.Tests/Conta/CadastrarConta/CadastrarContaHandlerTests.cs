using BankMore.Account.Application.Abstractions;
using BankMore.Account.Application.Conta.CadastrarConta;
using BankMore.Account.Application.Services.Idempotencia;
using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Entities;
using BankMore.Account.Domain.Repositories;
using Moq;
using System.Net;

namespace BankMore.Account.Tests.Conta.CadastrarConta;

public class CadastrarContaHandlerTests
{
    private readonly Mock<IContaCorrenteRepository> _contaRepositoryMock = new();
    private readonly Mock<IUsuarioRepository> _usuarioRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IIdempotenciaRepository> _idempotenciaRepositoryMock = new();
    private readonly Mock<IIdempotencyService> _idempotencyServiceMock = new();

    private readonly CadastrarContaHandler _handler;

    private const string CpfValido = "41083040049";
    private const string SenhaValida = "SenhaValida@123";

    public CadastrarContaHandlerTests()
    {
        _handler = new CadastrarContaHandler(
            _contaRepositoryMock.Object,
            _usuarioRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _idempotenciaRepositoryMock.Object,
            _idempotencyServiceMock.Object);
    }

    private void SetIdempotencyOk(Guid idempotencia, string cpf)
    {
        _idempotencyServiceMock
            .Setup(s => s.ChecIdempotenciakAsync(idempotencia, $"CadastroConta|{cpf}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, null));
    }

    private void SetupIdempotencyInvalid(Guid idempotencia, string cpf, ApiResult<object> retorno)
    {
        _idempotencyServiceMock
            .Setup(s => s.ChecIdempotenciakAsync(idempotencia, $"CadastroConta|{cpf}", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, retorno));
    }

    private void SetupTransactionOk_ExecutaDelegate()
    {
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(async (func, ct) =>
            {
                await func(ct);
                return TransactionResult.Ok();
            });
    }

    private void SetupTransactionFail(string message)
    {
        _unitOfWorkMock
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionResult.Fail(message));
    }

    private void SetupUsuarioEContaValidos(string cpf, ContaCorrente conta)
    {
        var usuario = new Usuario { ContaCorrente = conta };

        _usuarioRepositoryMock
            .Setup(r => r.GetByCpfAsync(cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);

        _contaRepositoryMock
            .Setup(r => r.GetAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conta);
    }

    [Fact]
    public async Task Handle_DeveRetornarResultadoIdempotencia_QuandoIdempotenciaInvalida_E_NaoChamarRepositorios()
    {
        var idem = Guid.NewGuid();
        var command = new CadastrarContaCommand { Cpf = CpfValido, Senha = SenhaValida, IdIdempotencia = idem };

        var resultadoErro = ApiResult<object>.Fail(HttpStatusCode.Conflict, AccountErrors.InvalidValue);
        SetupIdempotencyInvalid(idem, CpfValido, resultadoErro);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(resultadoErro.Status, result.Status);
        Assert.Equal(resultadoErro.Type, result.Type);

        _usuarioRepositoryMock.Verify(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _contaRepositoryMock.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
        _idempotenciaRepositoryMock.Verify(i => i.CreateAsync(It.IsAny<Idempotencia>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoCpfInvalido()
    {
        var idem = Guid.NewGuid();
        var command = new CadastrarContaCommand { Cpf = "123", Senha = SenhaValida, IdIdempotencia = idem };

        SetIdempotencyOk(idem, command.Cpf);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidDocument, result.Type);

        _usuarioRepositoryMock.Verify(r => r.GetByCpfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoUsuarioNaoEncontrado()
    {
        var idem = Guid.NewGuid();
        var command = new CadastrarContaCommand { Cpf = CpfValido, Senha = SenhaValida, IdIdempotencia = idem };

        SetIdempotencyOk(idem, CpfValido);

        _usuarioRepositoryMock
            .Setup(r => r.GetByCpfAsync(CpfValido, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidValue, result.Type);

        _contaRepositoryMock.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRetornarBadRequest_QuandoSenhaInvalida()
    {
        var idem = Guid.NewGuid();
        var command = new CadastrarContaCommand { Cpf = CpfValido, Senha = "123", IdIdempotencia = idem };

        SetIdempotencyOk(idem, CpfValido);

        SetupUsuarioEContaValidos(CpfValido, new ContaCorrente { IdContaCorrente = Guid.NewGuid() });

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(AccountErrors.InvalidValue, result.Type);

        _contaRepositoryMock.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never); 
        _passwordHasherMock.Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveAtualizarConta_E_SalvarIdempotencia_QuandoSucesso()
    {
        var idem = Guid.NewGuid();
        var command = new CadastrarContaCommand { Cpf = CpfValido, Senha = SenhaValida, IdIdempotencia = idem };

        SetIdempotencyOk(idem, CpfValido);

        var conta = new ContaCorrente
        {
            IdContaCorrente = Guid.NewGuid(),
            Salt = "oldSalt",
            Senha = "oldHash",
            Ativo = false
        };

        SetupUsuarioEContaValidos(CpfValido, conta);

        _passwordHasherMock
            .Setup(h => h.HashPassword(SenhaValida))
            .Returns(("novoHash", "novoSalt"));

        SetupTransactionOk_ExecutaDelegate();

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.Status);

        Assert.Equal("novoSalt", conta.Salt);
        Assert.Equal("novoHash", conta.Senha);
        Assert.True(conta.Ativo);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);

        _contaRepositoryMock.Verify(r =>
            r.GetAsync(conta.IdContaCorrente, It.IsAny<CancellationToken>()),
            Times.Once);

        _passwordHasherMock.Verify(h => h.HashPassword(SenhaValida), Times.Once);

        _contaRepositoryMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_DeveRetornarInternalServerError_QuandoTransacaoFalha_E_NaoSalvarIdempotencia()
    {
        var idem = Guid.NewGuid();
        var command = new CadastrarContaCommand { Cpf = CpfValido, Senha = SenhaValida, IdIdempotencia = idem };

        SetIdempotencyOk(idem, CpfValido);

        var conta = new ContaCorrente { IdContaCorrente = Guid.NewGuid(), Ativo = false };
        SetupUsuarioEContaValidos(CpfValido, conta);

        _passwordHasherMock
            .Setup(h => h.HashPassword(SenhaValida))
            .Returns(("hash", "salt"));

        SetupTransactionFail("erro transação");

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.Status);
        Assert.Equal(AccountErrors.InternalServerError, result.Type);
        Assert.Contains("erro transação", result.Message);

        _unitOfWorkMock.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
