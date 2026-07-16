using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// Bus -> browser. A service request was cancelled; tell the providers who work in that city so they can
/// stop tracking or bidding on it.
/// </summary>
public sealed class ServiceRequestCancelledRealtimeConsumer
    : AizenBaseMessageConsumer<ServiceRequestCancelledMessage>
{
    private readonly IHubContext<ProviderRealtimeHub> _hub;
    private readonly ILogger<ServiceRequestCancelledRealtimeConsumer> _logger;

    public ServiceRequestCancelledRealtimeConsumer(IServiceProvider sp) : base(sp)
    {
        _hub = sp.GetRequiredService<IHubContext<ProviderRealtimeHub>>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestCancelledRealtimeConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCancelledMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestCancelledMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.LocationCityCode))
        {
            _logger.LogInformation(
                "Service request {ServiceRequestId} cancelled with no city; no provider group to notify.",
                message.ServiceRequestId);
            return;
        }

        var group = ProviderRealtimeHub.CityGroup(message.LocationCityCode);

        await _hub.Clients.Group(group).SendAsync("providerEvent", new ProviderRealtimeEvent
        {
            EventType = ProviderRealtimeEventTypes.ServiceRequestCancelled,
            ServiceRequestId = message.ServiceRequestId,
            RequestCode = message.RequestCode,
            CityCode = message.LocationCityCode,
        }, ct);

        _logger.LogInformation("Pushed ServiceRequestCancelled {ServiceRequestId} to group {Group}.",
            message.ServiceRequestId, group);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestCancelledMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestCancelledRealtimeConsumer for SR {ServiceRequestId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
