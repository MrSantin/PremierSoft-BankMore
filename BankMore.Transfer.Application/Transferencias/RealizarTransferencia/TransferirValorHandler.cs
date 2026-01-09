using BankMore.Transfer.Application.Abstractions;
using BankMore.Transfer.Application.Clients.Accounts.Api;
using BankMore.Transfer.Application.Services.Idempotencia;
using BankMore.Transfer.Application.Shared;
using BankMore.Transfer.Domain.Entities;
using BankMore.Transfer.Domain.Repositories;
using System.Net;
using System.Text.Json;

namespace BankMore.Transfer.Application.Transferencias.RealizarTransferencia;

public class TransferirValorHandler : ITransferHandler<MovimentoContaCommand, ApiResult<object>>
{
    private readonly ITransferenciaRepository _repository;
    private readonly IIdempotenciaRepository _idempotenciaRepository;
    private readonly IIdempotencyService _idempotenciaService;
    private readonly IAccountApiClient _accountApi;
    private readonly IUnitOfWork _unitOfWork;

    public TransferirValorHandler(ITransferenciaRepository repository, IIdempotenciaRepository idempotenciaRepository, IIdempotencyService idempotencyService,
                                  IAccountApiClient accountApi, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _idempotenciaRepository = idempotenciaRepository;
        _idempotenciaService = idempotencyService;
        _accountApi = accountApi;
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResult<object>> Handle(MovimentoContaCommand request, CancellationToken ct)
    {
        var requisicao = $"Transferencia|{request.ContaOrigem}|{request.ContaDestino}|{request.Valor}";

        (bool idempotenciaValida, ApiResult<object>? resultadoIdempotencia) = await _idempotenciaService.ChecIdempotenciakAsync(request.IdIdempotencia, requisicao, ct);

        if (!idempotenciaValida)
            return resultadoIdempotencia!;

        if (request.Valor <= 0)
            return ApiResult<object>.Fail(HttpStatusCode.Forbidden, TransferErrors.InvalidValue);

        request.TipoMovimento = "C";
        var accountResp = await _accountApi.MovimentarContaAsync(request, ct);

        if (!accountResp.Success)
            return ApiResult<object>.Fail(accountResp.Status, accountResp.Type);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var resultado = JsonSerializer.Deserialize<ResultadoTransferenciaDto>(accountResp.Data.ToString(), options);

        var transferencia = new Transferencia
        {
            IdContaOrigem = request.ContaOrigem,
            IdContaDestino = resultado.IdContaDestino,
            DataMovimento = resultado.DataMovimento,
            Valor = request.Valor
        };

        var result = ApiResult<object>.NoContent();

        var idempotencia = new Idempotencia
        {
            ChaveIdempotencia = request.IdIdempotencia,
            Requisicao = requisicao,
            Resultado = JsonSerializer.Serialize(result)
        };
        try
        {
            var transactionResult = await _unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
            {
                await _idempotenciaRepository.CreateAsync(idempotencia, transactionCt);
                await _repository.CreateAsync(transferencia, transactionCt);
            }, ct);

            if (!transactionResult.Success)
                return ApiResult<object>.Fail(HttpStatusCode.InternalServerError, TransferErrors.InternalServerError, transactionResult.Message);
        }
        catch (Exception ex)
        {
            // Coloque um breakpoint aqui e veja o 'ex.InnerException'
            throw;
        }


        return result;
    }
}
