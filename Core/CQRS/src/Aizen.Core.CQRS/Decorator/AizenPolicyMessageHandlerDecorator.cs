using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Abstraction.Message;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Aizen.Core.CQRS.Decorator;

public class AizenPolicyMessageHandlerDecorator<TRequest, TResult> : IAizenRequestHandler<TRequest, TResult>,
    IAizenRequestDecorator<TRequest, TResult>
    where TResult : class where TRequest : IAizenRequest<TResult>
{
    public IAizenRequestHandler<TRequest, TResult> Decorated => _decorated;

    private readonly IAizenRequestHandler<TRequest, TResult> _decorated;

    private readonly AsyncTimeoutPolicy? _timeoutPolicy;
    private readonly AsyncCircuitBreakerPolicy? _circuitBreakerPolicy;
    private readonly AsyncRetryPolicy? _retryPolicy;

    private readonly List<Type> _exceptions;
    private readonly TResult? _result;

    public AizenPolicyMessageHandlerDecorator(
        IAizenRequestHandler<TRequest, TResult> decorated)
    {
        if (decorated is IAizenRequestDecorator<TRequest, TResult> requestHandler)
        {
            _decorated = requestHandler.Decorated;
        }
        else
        {
            _decorated = decorated;
        }

        if (_decorated is IAizenTimeoutPolicyHandler<TResult> timeoutPolicyHandler)
        {
            _timeoutPolicy =
                Policy.TimeoutAsync(timeoutPolicyHandler.Timeout, timeoutPolicyHandler.TimeoutStrategy);
            _exceptions = timeoutPolicyHandler.Exceptions;
            _result = timeoutPolicyHandler.Result;
        }

        if (_decorated is IAizenCircuitBreakerPolicyHandler<TResult> circuitBreakerPolicyHandler)
        {
            _circuitBreakerPolicy = Policy
                .Handle<Exception>(ex => circuitBreakerPolicyHandler.Exceptions.Any(x => x.IsInstanceOfType(ex)))
                .CircuitBreakerAsync(
                    circuitBreakerPolicyHandler.ExceptionsAllowedBeforeBreaking,
                    circuitBreakerPolicyHandler.DurationOfBreak);
            _exceptions = circuitBreakerPolicyHandler.Exceptions;
            _result = circuitBreakerPolicyHandler.Result;
        }

        if (_decorated is IAizenRetryPolicyHandler<TResult> retryPolicyHandler)
        {
            _retryPolicy = Policy
                .Handle<Exception>(ex => retryPolicyHandler.Exceptions.Any(x => x.IsInstanceOfType(ex)))
                .RetryAsync(retryPolicyHandler.RetryCount);
            _exceptions = retryPolicyHandler.Exceptions;
            _result = retryPolicyHandler.Result;
        }

        _decorated = decorated;
    }

    public async Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var policyWrap = Policy.WrapAsync(Policy.NoOpAsync<TResult>(), Policy.NoOpAsync<TResult>());
        if (_timeoutPolicy != null)
        {
            policyWrap = policyWrap.WrapAsync(_timeoutPolicy);
            if (_circuitBreakerPolicy != null)
            {
                policyWrap = policyWrap.WrapAsync(_circuitBreakerPolicy);
                if (_retryPolicy != null)
                {
                    policyWrap = policyWrap.WrapAsync(_retryPolicy);
                }
            }
        }
        else if (_circuitBreakerPolicy != null)
        {
            policyWrap = policyWrap.WrapAsync(_circuitBreakerPolicy);
            if (_retryPolicy != null)
            {
                policyWrap = policyWrap.WrapAsync(_retryPolicy);
            }
        }
        else if (_retryPolicy != null)
        {
            policyWrap = policyWrap.WrapAsync(_retryPolicy);
        }

        try
        {
            var result = await policyWrap.ExecuteAsync(() => _decorated.Handle(request, cancellationToken));
            return result;
        }
        catch (Exception e)
        {
            Elastic.Apm.Agent.Tracer?.CurrentTransaction?.CaptureException(e);
            if (_result != null && _exceptions.Any(x => x.IsInstanceOfType(e)))
            {
                return _result;
            }

            throw;
        }
    }
}