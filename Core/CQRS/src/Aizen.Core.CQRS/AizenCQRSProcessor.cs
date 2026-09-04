using MediatR;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS;

internal sealed class AizenCQRSProcessor : IAizenCQRSProcessor
{
    private readonly IMediator _mediator;

    public AizenCQRSProcessor(IMediator mediator)
    {
        this._mediator = mediator;
    }

    public Task ProcessAsync(IAizenCommand command, CancellationToken cancellationToken)
    {
        return this._mediator.Send(command, cancellationToken);
    }

    public Task<TResult> ProcessAsync<TResult>(IAizenRequest<TResult> request, CancellationToken cancellationToken)
    {
        return this._mediator.Send(request, cancellationToken);
    }

    public Task<TResult> ProcessAsync<TResult>(IAizenCommand<TResult> command, CancellationToken cancellationToken)
    {
        return this._mediator.Send(command, cancellationToken);
    }

    public Task<TResult> ProcessAsync<TResult>(IAizenQuery<TResult> query, CancellationToken cancellationToken)
    {
        return this._mediator.Send(query, cancellationToken);
    }
}