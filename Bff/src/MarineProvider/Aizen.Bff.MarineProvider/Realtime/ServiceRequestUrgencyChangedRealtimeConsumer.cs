using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Realtime;

/// <summary>
/// Bus -> browser. A service request's priority changed; tell the providers who work in that city so
/// they can re-evaluate whether to bid or re-prioritise existing work.
/// </summary>
public sealed class ServiceRequestUrgencyChangedRealtimeConsumer
    : AizenBaseMessageConsumer<ServiceRequestUrgencyChangedMessage>
{
    private readonly IHubContext<ProviderRealtimeHub> _hub;
    private readonly ILogger<ServiceRequestUrgencyChangedRealtimeConsumer> _logger;

    public ServiceRequestUrgencyChangedRealtimeConsumer(IServiceProvider sp) : base(sp)
    {
        _hub = sp.GetRequiredService<IHubContext<ProviderRealtimeHub>>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestUrgencyChangedRealtimeConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestUrgencyChangedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestUrgencyChangedMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.LocationCityCode))
        {
            _logger.LogInformation(
                "Service request {ServiceRequestId} urgency changed with no city; no provider group to notify.",
                message.ServiceRequestId);
            return;
        }

        var group = ProviderRealtimeHub.CityGroup(message.LocationCityCode);

        await _hub.Clients.Group(group).SendAsync("providerEvent", new ProviderRealtimeEvent
        {
            EventType = ProviderRealtimeEventTypes.ServiceRequestUrgencyChanged,
            ServiceRequestId = message.ServiceRequestId,
            RequestCode = message.RequestCode,
            CityCode = message.LocationCityCode,
        }, ct);

        _logger.LogInformation("Pushed ServiceRequestUrgencyChanged {ServiceRequestId} to group {Group}.",
            message.ServiceRequestId, group);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestUrgencyChangedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestUrgencyChangedRealtimeConsumer for SR {ServiceRequestId}: {Error}",
            message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
