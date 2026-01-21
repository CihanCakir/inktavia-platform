namespace Aizen.Core.EventStore.Abstraction;
public interface IEventStorePublisher
{
    Task PopulateAsync<T>(T source, CancellationToken cancellationToken) where T : BaseEvent<T>;
}