using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// Bus → browser. A message was added to a conversation involving this provider.
/// Only notifies the provider for Owner→provider messages (provider's own sends don't toast).
/// No message content on the frame — the SPA refetches the thread.
/// </summary>
public sealed class MessageAddedRealtimeConsumer
    : AizenBaseMessageConsumer<ServiceRequestMessageSentMessage>
{
    private readonly IHubContext<ProviderRealtimeHub> _hub;
    private readonly ILogger<MessageAddedRealtimeConsumer> _logger;

    public MessageAddedRealtimeConsumer(IServiceProvider sp) : base(sp)
    {
        _hub = sp.GetRequiredService<IHubContext<ProviderRealtimeHub>>();
        _logger = sp.GetRequiredService<ILogger<MessageAddedRealtimeConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestMessageSentMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestMessageSentMessage message, CancellationToken ct)
    {
        // Notify the provider for Owner messages (counterparty) and System messages (lifecycle pills)
        if (message.SenderType != ServiceRequestMessageSenderType.Owner
            && message.SenderType != ServiceRequestMessageSenderType.System)
            return;

        if (!message.ProviderProfileId.HasValue || message.ProviderProfileId.Value <= 0)
        {
            _logger.LogWarning("MessageSent for SR {ServiceRequestId} carries no provider profile id; nothing to notify.",
                message.ServiceRequestId);
            return;
        }

        var group = ProviderRealtimeHub.ProviderGroup(message.ProviderProfileId.Value);

        await _hub.Clients.Group(group).SendAsync("providerEvent", new ProviderRealtimeEvent
        {
            EventType = ProviderRealtimeEventTypes.MessageAdded,
            ServiceRequestId = message.ServiceRequestId,
            MessageSenderType = message.SenderType.ToString(),
        }, ct);

        _logger.LogInformation("Pushed MessageAdded for SR {ServiceRequestId} to group {Group}.",
            message.ServiceRequestId, group);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestMessageSentMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: MessageAddedRealtimeConsumer for SR {ServiceRequestId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
