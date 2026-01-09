using BankMore.Account.Application.Shared;
using BankMore.Account.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BankMore.Account.Application.Services.Idempotencia;

public class IdempotencyService : IIdempotencyService
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly IIdempotenciaRepository _repository;
    public IdempotencyService(IIdempotenciaRepository repository) => _repository = repository;

    public async Task<(bool IsValid, ApiResult<object>? Result)> ChecIdempotenciakAsync(Guid chaveIdempotencia, string requisicao, CancellationToken ct)
    {
        var idempotencia = await _repository.GetAsync(chaveIdempotencia, ct);

        if (idempotencia == null) return (true, null);

        if (idempotencia.Requisicao == requisicao)
        {
            return (false, JsonSerializer.Deserialize<ApiResult<object>>(idempotencia.Resultado, _jsonOptions));
        }

        return (false, ApiResult<object>.Fail(
            HttpStatusCode.Conflict,
            AccountErrors.Forbidden,
            "Idempotency-Key reutilizada com dados diferentes"
        ));
    }
}