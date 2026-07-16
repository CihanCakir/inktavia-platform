using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// Bus → browser. The provider's offer was rejected by the owner.
/// Addressed to exactly one provider group.
/// </summary>
public sealed class OfferRejectedRealtimeConsumer
    : AizenBaseMessageConsumer<ServiceRequestOfferRejectedMessage>
{
    private readonly IHubContext<ProviderRealtimeHub> _hub;
    private readonly ILogger<OfferRejectedRealtimeConsumer> _logger;

    public OfferRejectedRealtimeConsumer(IServiceProvider sp) : base(sp)
    {
        _hub = sp.GetRequiredService<IHubContext<ProviderRealtimeHub>>();
        _logger = sp.GetRequiredService<ILogger<OfferRejectedRealtimeConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestOfferRejectedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestOfferRejectedMessage message, CancellationToken ct)
    {
        if (message.ProviderProfileId <= 0)
        {
            _logger.LogWarning("OfferRejected for offer {OfferId} carries no provider profile id; nothing to notify.",
                message.OfferId);
            return;
        }

        var group = ProviderRealtimeHub.ProviderGroup(message.ProviderProfileId);

        await _hub.Clients.Group(group).SendAsync("providerEvent", new ProviderRealtimeEvent
        {
            EventType = ProviderRealtimeEventTypes.OfferRejected,
            ServiceRequestId = message.ServiceRequestId,
            OfferId = message.OfferId,
        }, ct);

        _logger.LogInformation("Pushed OfferRejected {OfferId} to group {Group}.", message.OfferId, group);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestOfferRejectedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: OfferRejectedRealtimeConsumer for offer {OfferId}: {Error}", message.OfferId, ex.Message);
        return Task.CompletedTask;
    }
}
