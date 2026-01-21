namespace Aizen.Core.EventStore.Abstraction.Projection;

public interface IEventStoreProjectionManager
{
    public Task<Tuple<bool, TState>> TryGetStateAsync<TState>(string name,
        CancellationToken cancellationToken)
        where TState : class;

    public Task<Tuple<bool, TState>> TryGetStateAsync<TState>(string name, string partition,
        CancellationToken cancellationToken)
        where TState : class;
}