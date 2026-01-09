using BankMore.Transfer.Application.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BankMore.Transfer.Api.Controllers.Shared;

[ApiController]
public abstract class TransferControllerBase : ControllerBase
{
    protected IActionResult FromResult<T>(ApiResult<T> result)
    {
        if (result.Success)
        {
            if (result.Status == HttpStatusCode.NoContent)
                return NoContent();

            return StatusCode((int)result.Status, result);
        }

        return StatusCode((int)result.Status, result);
    }
}