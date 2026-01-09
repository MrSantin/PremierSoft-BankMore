using BankMore.Transfer.Domain.Entities;
using MediatR;

namespace BankMore.Transfer.Application.Shared;

public interface ITransferCommand : IRequest<ApiResult<object>>
{
}
