using BankMore.Account.Application.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Application.Services.Idempotencia;

public interface IIdempotencyService : IApplicationService
{
    Task<(bool IsValid, ApiResult<object>? Result)> ChecIdempotenciakAsync(Guid chaveIdempotencia, string requisicao, CancellationToken ct);
}
