using MediatR;

namespace Aizen.Core.CQRS.Abstraction.Message;

public interface IAizenRequest<out TResult> : IRequest<TResult>
{
    
}