using BankMore.Account.Api.Controllers.Shared;
using BankMore.Account.Api.Extensions;
using BankMore.Account.Application.Conta.CadastrarConta;
using BankMore.Account.Application.Conta.InativarConta;
using BankMore.JwtService.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankMore.Account.Api.Controllers;

[Route("api/v1/[controller]")]
public class ContaCorrenteController : AccountControllerBase
{
    private readonly IMediator _mediator;
    public ContaCorrenteController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpPost("cadastrarconta")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CadastrarConta([FromBody] CadastrarContaCommand request, CancellationToken ct)
    {
        var result = await _mediator.Send(request, ct);
        return FromResult(result);
    }

    [Authorize]
    [HttpPost("inativarconta")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InativarConta([FromBody] InativarContaCommand request, CancellationToken ct)
    {
        request.IdConta = User.GetContaId();

        var result = await _mediator.Send(request, ct);
        return FromResult(result);
    }
}

