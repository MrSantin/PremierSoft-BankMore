using BankMore.Account.Api.Controllers.Shared;
using BankMore.Account.Application.MovimentoConta.Movimentacao;
using BankMore.Account.Application.MovimentoConta.Saldo;
using BankMore.JwtService.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMore.Account.Api.Controllers;

[Route("api/v1/[controller]")]
public class MovimentoContaController : AccountControllerBase
{
    private readonly IMediator _mediator;
    public MovimentoContaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpPost("movimentarconta")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MovimentarConta([FromBody] MovimentoContaCommand request, CancellationToken ct)
    {
        request.ContaOrigem = User.GetContaId();
        var result = await _mediator.Send(request, ct);
        return FromResult(result);
    }


    [Authorize]
    [HttpGet("consultarsaldo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ConsultarSaldo(CancellationToken ct)
    {
        var request = new ConsultaSaldoCommand
        {
            IdConta = User.GetContaId()
        };
        var result = await _mediator.Send(request, ct);
        return FromResult(result);
    }


}

