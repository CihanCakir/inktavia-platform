using Aizen.Core.CQRS.Abstraction.Message;
using Polly.Timeout;

namespace Aizen.Core.CQRS.Abstraction.Handler;

public interface IAizenTimeoutPolicyHandler<out TResult> 
    where TResult : class
{
    List<Type> Exceptions { get; }
    
    TimeoutStrategy TimeoutStrategy { get; }
    
    TimeSpan Timeout { get; }
    
    TResult? Result { get; }
}

public interface IAizenCircuitBreakerPolicyHandler<out TResult> 
    where TResult : class
{
    List<Type> Exceptions { get; }
    
    int ExceptionsAllowedBeforeBreaking { get; }
    
    TimeSpan DurationOfBreak { get; }
    
    TResult? Result { get; }
}

public interface IAizenRetryPolicyHandler<out TResult> 
    where TResult : class
{
    List<Type> Exceptions { get; }
    
    int RetryCount { get; }
    
    TResult? Result { get; }
}