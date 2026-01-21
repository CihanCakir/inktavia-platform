using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS.Handler;

public abstract class AizenQueryHandler<TQuery, TResult> : IAizenQueryHandler<TQuery, TResult>
    where TQuery : IAizenQuery<TResult>
{
    public abstract Task<TResult> Handle(TQuery request, CancellationToken cancellationToken);
}