using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Aizen.Core.Realtime.Abstraction.Interfaces;

namespace Aizen.Core.Realtime.Hubs;
/// <summary>
/// Domain-oriented hub base.
/// </summary>
public abstract class DomainHubBase : Hub
{
    protected readonly IRealtimePublisher _publisher;
    public abstract string DomainName { get; }

    protected DomainHubBase(IRealtimePublisher publisher)
    {
        _publisher = publisher;
    }

    protected static string ChannelForAggregate(string domain, string aggregateId) => $"{domain}:{aggregateId}";
    protected static string OrganizerChannel(string domain, string aggregateId) => $"{domain}:{aggregateId}:organizer";
    protected static string ActivityChatChannel(string domain, string aggregateId) => $"{domain}:{aggregateId}:chat";
    protected static string UserChannel(string userId) => $"user:{userId}";

    protected Task PublishToChannelAsync(string channel, object payload) =>
        _publisher.PublishToChannelAsync(channel, payload);

    protected Task PublishToUserAsync(string userId, object payload) =>
        _publisher.PublishToUserAsync(userId, payload);

    protected Task PublishToGroupAsync(string group, object payload) =>
        _publisher.PublishToGroupAsync(group, payload);
}