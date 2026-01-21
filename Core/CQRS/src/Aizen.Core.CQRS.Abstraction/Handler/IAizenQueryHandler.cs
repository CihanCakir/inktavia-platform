using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS.Abstraction.Handler;

public interface IAizenQueryHandler<in TQuery, TResult> : IAizenRequestHandler<TQuery, TResult>
    where TQuery : IAizenQuery<TResult>
{
    
}

public interface IAizenGenericHandler
{
    
}