using System.Text;
using EventStore.Client;
using Aizen.Core.EventStore.Abstraction;
using Aizen.Core.EventStore.Abstraction.Projection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Aizen.Core.EventStore;

internal class EventStorePublisher : IEventStorePublisher, IEventStoreProjectionManager
{
    private readonly EventStoreClient _eventStoreClient;

    private readonly EventStoreProjectionManagementClient _projectionManagementClient;

    private readonly JsonSerializerSettings _serializerSettings;

    public EventStorePublisher(EventStoreClient eventStoreClient,
        EventStoreProjectionManagementClient projectionManagementClient)
    {
        _eventStoreClient = eventStoreClient;
        _projectionManagementClient = projectionManagementClient;

        _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
        _serializerSettings.Converters.Add(new StringEnumConverter());
    }

    public async Task PopulateAsync<T>(T source, CancellationToken cancellationToken) where T : BaseEvent<T>
    {
        try
        {
            var streamName = $"TransactionType-{source.TransactionType}";
            var metadata =
                await _eventStoreClient.GetStreamMetadataAsync(streamName, cancellationToken: cancellationToken);
            if (metadata.Metadata.MaxAge != source.TimeToLive)
            {
                await _eventStoreClient.SetStreamMetadataAsync(
                    streamName,
                    StreamState.Any,
                    new StreamMetadata(
                        maxAge: source.TimeToLive
                    ), cancellationToken: cancellationToken);
            }


            var json = JsonConvert.SerializeObject(source, Formatting.Indented, _serializerSettings);
            var eventPayload = new EventData(
                source.EventId,
                source.EventType,
                Encoding.UTF8.GetBytes(json)
            );

            await _eventStoreClient.AppendToStreamAsync(
                streamName: streamName,
                expectedState: StreamState.Any,
                eventData: new[] { eventPayload },
                configureOperationOptions: options => { options.ThrowOnAppendFailure = false; },
                deadline: source.TimeToLive,
                cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            Elastic.Apm.Agent.Tracer.CurrentTransaction?.CaptureException(e);
        }
    }

    public async Task<Tuple<bool, TState>> TryGetStateAsync<TState>(
        string name, 
        CancellationToken cancellationToken)
        where TState : class
    {
        try
        {
            var stateObj = await _projectionManagementClient.GetResultAsync(
                name,
                cancellationToken: cancellationToken);

            var stateString = System.Text.Json.JsonSerializer.Serialize(stateObj);
            var state = JsonConvert.DeserializeObject<TState>(stateString, _serializerSettings);

            return new Tuple<bool, TState>(true, state);
        }
        catch (Exception e)
        {
            Elastic.Apm.Agent.Tracer.CurrentTransaction?.CaptureException(e);

            return new Tuple<bool, TState>(false, default);
        }
    }

    public async Task<Tuple<bool, TState>> TryGetStateAsync<TState>(
        string name, string partition,
        CancellationToken cancellationToken) where TState : class
    {
        try
        {
            var stateObj = await _projectionManagementClient.GetResultAsync(
                name,
                partition,
                cancellationToken: cancellationToken);

            var stateString = System.Text.Json.JsonSerializer.Serialize(stateObj);
            var state = JsonConvert.DeserializeObject<TState>(stateString, _serializerSettings);

            return new Tuple<bool, TState>(true, state);
        }
        catch (Exception e)
        {
            Elastic.Apm.Agent.Tracer.CurrentTransaction?.CaptureException(e);
            return new Tuple<bool, TState>(false, default);
        }
    }
}