using MediatR;
using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS.Abstraction.Handler;

public interface IAizenRequestHandler<in TRequest, TResult> : IRequestHandler<TRequest, TResult>
    where TRequest : IAizenRequest<TResult>
{
    
}