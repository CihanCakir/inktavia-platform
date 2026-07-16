using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// Bus -> browser. A service request's details were updated; tell the providers who work in that city.
/// </summary>
public sealed class ServiceRequestUpdatedRealtimeConsumer
    : AizenBaseMessageConsumer<ServiceRequestUpdatedMessage>
{
    private readonly IHubContext<ProviderRealtimeHub> _hub;
    private readonly ILogger<ServiceRequestUpdatedRealtimeConsumer> _logger;

    public ServiceRequestUpdatedRealtimeConsumer(IServiceProvider sp) : base(sp)
    {
        _hub = sp.GetRequiredService<IHubContext<ProviderRealtimeHub>>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestUpdatedRealtimeConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestUpdatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestUpdatedMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.LocationCityCode))
        {
            _logger.LogInformation(
                "Service request {ServiceRequestId} updated with no city; no provider group to notify.",
                message.ServiceRequestId);
            return;
        }

        var group = ProviderRealtimeHub.CityGroup(message.LocationCityCode);

        await _hub.Clients.Group(group).SendAsync("providerEvent", new ProviderRealtimeEvent
        {
            EventType = ProviderRealtimeEventTypes.ServiceRequestUpdated,
            ServiceRequestId = message.ServiceRequestId,
            RequestCode = message.RequestCode,
            CityCode = message.LocationCityCode,
        }, ct);

        _logger.LogInformation("Pushed ServiceRequestUpdated {ServiceRequestId} to group {Group}.",
            message.ServiceRequestId, group);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestUpdatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestUpdatedRealtimeConsumer for SR {ServiceRequestId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
