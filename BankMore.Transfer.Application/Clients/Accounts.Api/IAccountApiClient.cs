using BankMore.Transfer.Application.Shared;
using BankMore.Transfer.Application.Transferencias.RealizarTransferencia;
using Refit;

namespace BankMore.Transfer.Application.Clients.Accounts.Api;

public interface IAccountApiClient
{

    [Post("/MovimentoConta/movimentarconta")]
    Task<ApiResult<object>> MovimentarContaAsync([Body] MovimentoContaCommand command, CancellationToken ct);
}
