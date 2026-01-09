using MediatR;

namespace BankMore.Account.Application.Shared;

public interface IAccountHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
where TCommand : IRequest<TResponse>
{
}
