using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace BankMore.Account.Application.Shared;

public interface IAccountCommand : IRequest<ApiResult<object>>
{
}
