using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS.Handler;

public abstract class AizenCommandHandler<TCommand, TResult> : IAizenCommandHandler<TCommand, TResult>
    where TCommand : IAizenCommand<TResult>
{
    public virtual bool IsTransactional => true;
    
    public abstract Task<TResult?> Handle(TCommand request, CancellationToken cancellationToken);
}