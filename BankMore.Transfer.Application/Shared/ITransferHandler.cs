using MediatR;

namespace BankMore.Transfer.Application.Shared;

public interface ITransferHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
where TCommand : IRequest<TResponse>
{
}
