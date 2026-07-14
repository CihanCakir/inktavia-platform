using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// Bus → browser. A request became biddable; tell the providers who work in that city.
///
/// Fan-out is by CITY GROUP, not by broadcast. Pushing every published request to every connected provider and
/// letting the client filter would hand each provider the entire national demand feed — including cities they have
/// no business seeing. The group is joined server-side from the provider's own profile.
/// </summary>
public sealed class ServiceRequestPublishedRealtimeConsumer
    : AizenBaseMessageConsumer<ServiceRequestPublishedMessage>
{
    private readonly IHubContext<ProviderRealtimeHub> _hub;
    private readonly ILogger<ServiceRequestPublishedRealtimeConsumer> _logger;

    public ServiceRequestPublishedRealtimeConsumer(IServiceProvider sp) : base(sp)
    {
        _hub = sp.GetRequiredService<IHubContext<ProviderRealtimeHub>>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestPublishedRealtimeConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestPublishedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestPublishedMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.LocationCityCode))
        {
            // No city means nobody to address. Dropping it is correct — broadcasting it to everyone is not.
            _logger.LogInformation(
                "Service request {ServiceRequestId} published with no city; no provider group to notify.",
                message.ServiceRequestId);
            return;
        }

        var group = ProviderRealtimeHub.CityGroup(message.LocationCityCode);

        await _hub.Clients.Group(group).SendAsync("providerEvent", new ProviderRealtimeEvent
        {
            EventType = ProviderRealtimeEventTypes.ServiceRequestPublished,
            ServiceRequestId = message.ServiceRequestId,
            RequestCode = message.RequestCode,
            Title = message.Title,
            CityCode = message.LocationCityCode,
            MarinaName = message.LocationMarinaName,
            OccurredAt = message.PublishedAt,
        }, ct);

        _logger.LogInformation("Pushed ServiceRequestPublished {ServiceRequestId} to group {Group}.",
            message.ServiceRequestId, group);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestPublishedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestPublishedRealtimeConsumer for SR {ServiceRequestId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
