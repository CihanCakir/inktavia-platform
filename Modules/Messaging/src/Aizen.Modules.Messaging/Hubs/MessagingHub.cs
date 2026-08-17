using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Aizen.Modules.Messaging.Hubs;

[DocumentationInfo("Messaging Hub",
    "SignalR hub for real-time messaging events across all conversation contexts.")]
[Authorize]
public sealed class MessagingHub : DomainHubBase
{
    public override string DomainName => "messaging";

    public MessagingHub(IRealtimePublisher publisher) : base(publisher) { }

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

    public async Task JoinConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"messaging:conv:{conversationId}");
    }

    public async Task LeaveConversation(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) return;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"messaging:conv:{conversationId}");
    }

    [Authorize(Roles = "Admin")]
    public async Task SubscribeToModerationQueue()
        => await Groups.AddToGroupAsync(Context.ConnectionId, "admin:messaging-moderation");

    [Authorize(Roles = "Admin")]
    public async Task UnsubscribeFromModerationQueue()
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin:messaging-moderation");
}

[DocumentationInfo("Messaging hub events", "Server-to-client event name constants for the messaging hub.")]
public static class MessagingHubEvents
{
    public const string MessageReceived           = "MessageReceived";
    public const string ConversationCreated       = "ConversationCreated";
    public const string ConversationStatusChanged = "ConversationStatusChanged";
    public const string MessageModerated          = "MessageModerated";
    public const string UnreadCountUpdated        = "UnreadCountUpdated";
    public const string ParticipantJoined         = "ParticipantJoined";
}
