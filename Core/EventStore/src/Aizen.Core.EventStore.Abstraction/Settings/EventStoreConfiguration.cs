namespace Aizen.Core.EventStore.Abstraction
{
    public class EventStoreConfiguration
    {
        public string ConnectionString { get; set; }
        public RemoteCallConfiguration RemoteCalls { get; set; }
    }

    public class RemoteCallConfiguration
    {
        public EventSourceConnectConfiguration IEventSourceConnect { get; set; }
    }

    public class EventSourceConnectConfiguration
    {
        public string BaseUrl { get; set; }
        public IDictionary<string, string> DefaultHeaders { get; set; }
    }
}
