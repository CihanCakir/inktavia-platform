using Aizen.Core.Realtime.Hubs;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aizen.Modules.ServiceRequest.Hubs;

[DocumentationInfo("ServiceRequest Hub", "SignalR hub for real-time service request lifecycle events.")]
[Authorize]
public sealed class ServiceRequestHub : DomainHubBase
{
    public override string DomainName => "servicerequest";

    public ServiceRequestHub(IRealtimePublisher publisher) : base(publisher) { }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Subscribes the connection to a specific service request channel.</summary>
    public async Task SubscribeToServiceRequest(string serviceRequestId)
    {
        if (string.IsNullOrWhiteSpace(serviceRequestId)) return;
        var channel = ChannelForAggregate(DomainName, serviceRequestId);
        await Groups.AddToGroupAsync(Context.ConnectionId, channel);
    }

    /// <summary>Unsubscribes from a specific service request channel.</summary>
    public async Task UnsubscribeFromServiceRequest(string serviceRequestId)
    {
        if (string.IsNullOrWhiteSpace(serviceRequestId)) return;
        var channel = ChannelForAggregate(DomainName, serviceRequestId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, channel);
    }

    /// <summary>Subscribes to the provider's own channel.</summary>
    public async Task SubscribeAsProvider(string providerProfileId)
    {
        if (string.IsNullOrWhiteSpace(providerProfileId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"provider:{providerProfileId}");
    }

    /// <summary>Admin subscribes to the operations channel.</summary>
    [Authorize(Roles = "Admin")]
    public async Task SubscribeToAdminOperations()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin:operations");
    }

    /// <summary>Subscribes to a conversation channel.</summary>
    public async Task JoinConversationGroup(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
    }

    /// <summary>Unsubscribes from a conversation channel.</summary>
    public async Task LeaveConversationGroup(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv:{conversationId}");
    }
}

public static class SrHubEvents
{
    public const string WorkLogEntryAdded = "WorkLogEntryAdded";
    public const string StatusChanged = "StatusChanged";
    public const string OfferAccepted = "OfferAccepted";
    public const string DisputeFiled = "DisputeFiled";
    public const string PaymentReleased = "PaymentReleased";
    public const string MessageReceived = "MessageReceived";
}
