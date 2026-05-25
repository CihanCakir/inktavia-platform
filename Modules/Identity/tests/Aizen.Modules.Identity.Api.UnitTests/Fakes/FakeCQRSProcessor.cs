using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Modules.Identity.Api.UnitTests.Fakes;

public class FakeCQRSProcessor : IAizenCQRSProcessor
{
    private readonly Dictionary<Type, object> _results = new();
    public List<object> ProcessedCommands { get; } = new();

    public void SetupResult<TResult>(TResult result)
    {
        _results[typeof(TResult)] = result!;
    }

    public Task ProcessAsync(IAizenCommand command, CancellationToken cancellationToken)
    {
        ProcessedCommands.Add(command);
        return Task.CompletedTask;
    }

    public Task<TResult> ProcessAsync<TResult>(IAizenRequest<TResult> request, CancellationToken cancellationToken)
    {
        ProcessedCommands.Add(request);
        if (_results.TryGetValue(typeof(TResult), out var result))
            return Task.FromResult((TResult)result);
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult> ProcessAsync<TResult>(IAizenCommand<TResult> command, CancellationToken cancellationToken)
    {
        ProcessedCommands.Add(command);
        if (_results.TryGetValue(typeof(TResult), out var result))
            return Task.FromResult((TResult)result);
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult> ProcessAsync<TResult>(IAizenQuery<TResult> query, CancellationToken cancellationToken)
    {
        ProcessedCommands.Add(query);
        if (_results.TryGetValue(typeof(TResult), out var result))
            return Task.FromResult((TResult)result);
        return Task.FromResult(default(TResult)!);
    }
}
