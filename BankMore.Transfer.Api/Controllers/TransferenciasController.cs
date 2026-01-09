using BankMore.JwtService.Extensions;
using BankMore.Transfer.Api.Controllers.Shared;
using BankMore.Transfer.Application.Transferencias.RealizarTransferencia;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMore.Transfer.Api.Controllers;

[Route("api/v1/[controller]")]
public class TransferenciasController : TransferControllerBase
{
    private readonly IMediator _mediator;
    public TransferenciasController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [Authorize]
    [HttpPost("transferirvalor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TransferirValor([FromBody] TransferirValorCommand request, CancellationToken ct)
    {
        var command = new MovimentoContaCommand
        {
            ContaDestino = request.ContaDestino,
            IdIdempotencia = request.IdIdempotencia,
            Valor = request.Valor
        };

        var result = await _mediator.Send(command, ct);
        return FromResult(result);
    }
}
