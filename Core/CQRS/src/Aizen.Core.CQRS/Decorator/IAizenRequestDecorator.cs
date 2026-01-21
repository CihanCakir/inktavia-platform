using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS.Decorator;

public interface IAizenRequestDecorator<TRequest, TResult> : IAizenRequestHandler<TRequest, TResult>
    where TRequest : IAizenRequest<TResult>
{
    IAizenRequestHandler<TRequest, TResult> Decorated { get; }
}