using BankMore.Transfer.Application.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Transfer.Application.Services.Idempotencia;

public interface IIdempotencyService : ITransferService
{
    Task<(bool IsValid, ApiResult<object>? Result)> ChecIdempotenciakAsync(Guid chaveIdempotencia, string requisicao, CancellationToken ct);
}
