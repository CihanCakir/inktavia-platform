using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS.Abstraction;

public interface IAizenCQRSProcessor
{
    Task ProcessAsync(IAizenCommand command, CancellationToken cancellationToken);

    Task<TResult> ProcessAsync<TResult>(IAizenRequest<TResult> request, CancellationToken cancellationToken);

    Task<TResult> ProcessAsync<TResult>(IAizenCommand<TResult> command, CancellationToken cancellationToken);

    Task<TResult> ProcessAsync<TResult>(IAizenQuery<TResult> query, CancellationToken cancellationToken);
}