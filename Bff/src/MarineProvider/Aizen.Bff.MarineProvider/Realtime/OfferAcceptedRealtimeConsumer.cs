using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// Bus → browser. The provider's bid was accepted.
///
/// Addressed to exactly one provider group. This event says "you won the job" and must never reach anyone else:
/// knowing that a competitor's offer was accepted, and for which request, is commercially sensitive.
/// </summary>
public sealed class OfferAcceptedRealtimeConsumer
    : AizenBaseMessageConsumer<ServiceRequestOfferAcceptedMessage>
{
    private readonly IHubContext<ProviderRealtimeHub> _hub;
    private readonly ILogger<OfferAcceptedRealtimeConsumer> _logger;

    public OfferAcceptedRealtimeConsumer(IServiceProvider sp) : base(sp)
    {
        _hub = sp.GetRequiredService<IHubContext<ProviderRealtimeHub>>();
        _logger = sp.GetRequiredService<ILogger<OfferAcceptedRealtimeConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestOfferAcceptedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestOfferAcceptedMessage message, CancellationToken ct)
    {
        if (message.ProviderProfileId <= 0)
        {
            _logger.LogWarning("OfferAccepted for offer {OfferId} carries no provider profile id; nothing to notify.",
                message.OfferId);
            return;
        }

        var group = ProviderRealtimeHub.ProviderGroup(message.ProviderProfileId);

        await _hub.Clients.Group(group).SendAsync("providerEvent", new ProviderRealtimeEvent
        {
            EventType = ProviderRealtimeEventTypes.OfferAccepted,
            ServiceRequestId = message.ServiceRequestId,
            OfferId = message.OfferId,
        }, ct);

        _logger.LogInformation("Pushed OfferAccepted {OfferId} to group {Group}.", message.OfferId, group);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestOfferAcceptedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: OfferAcceptedRealtimeConsumer for offer {OfferId}: {Error}", message.OfferId, ex.Message);
        return Task.CompletedTask;
    }
}
