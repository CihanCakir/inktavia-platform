using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS.Abstraction.Handler;


public interface IAizenCommandHandler<in TCommand, TResult> : IAizenRequestHandler<TCommand, TResult>
    where TCommand : IAizenCommand<TResult>
{
    bool IsTransactional { get; }
}