using BankMore.Account.Application.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BankMore.Account.Api.Controllers.Shared;

[ApiController]
public abstract class AccountControllerBase : ControllerBase
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

