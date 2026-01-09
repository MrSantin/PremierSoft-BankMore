using BankMore.Account.Api.Controllers.Shared;
using BankMore.Account.Application.Usuarios.CadastrarUsuario;
using BankMore.Account.Application.Usuarios.Login;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankMore.Account.Api.Controllers;

[Route("api/v1/[controller]")]
public class UsuarioController : AccountControllerBase
{
    private readonly IMediator _mediator;

    public UsuarioController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand request, CancellationToken ct)
    {
        var result = await _mediator.Send(request, ct);
        return FromResult(result);
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register([FromBody] CadastrarUsuarioCommand request, CancellationToken ct)
    {
        var result = await _mediator.Send(request, ct);
        return FromResult(result);
    }
}

