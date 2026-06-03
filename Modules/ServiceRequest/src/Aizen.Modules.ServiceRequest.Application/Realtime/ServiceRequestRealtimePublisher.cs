using System.Text.Json;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;

namespace Aizen.Modules.ServiceRequest.Application.Realtime;

[DocumentationInfo("ServiceRequest realtime publisher", "Helper for publishing typed ServiceRequest realtime events to the correct channels.")]
public sealed class ServiceRequestRealtimePublisher
{
    private readonly IRealtimePublisher _publisher;

    public ServiceRequestRealtimePublisher(IRealtimePublisher publisher) => _publisher = publisher;

    public async Task PublishAsync(
        long serviceRequestId,
        string requestCode,
        long ownerUserId,
        long? providerProfileId,
        ServiceRequestRealtimeEventType eventType,
        object payload,
        long? actorUserId = null,
        ServiceRequestActorType? actorType = null,
        CancellationToken ct = default)
    {
        var dto = new ServiceRequestRealtimeEventDto
        {
            ServiceRequestId = serviceRequestId,
            RequestCode = requestCode,
            EventType = eventType,
            PayloadType = payload.GetType().Name,
            PayloadJson = JsonSerializer.Serialize(payload),
            OccurredAt = DateTimeOffset.UtcNow,
            ActorUserId = actorUserId,
            ActorType = actorType
        };

        await _publisher.PublishToGroupAsync($"servicerequest:{serviceRequestId}", dto, ct: ct);
        await _publisher.PublishToUserAsync(ownerUserId.ToString(), dto, ct: ct);

        if (providerProfileId.HasValue)
            await _publisher.PublishToGroupAsync($"provider:{providerProfileId.Value}", dto, ct: ct);

        if (eventType is ServiceRequestRealtimeEventType.AdminInterventionRequired
            or ServiceRequestRealtimeEventType.DisputeOpened
            or ServiceRequestRealtimeEventType.DisputeStatusChanged)
        {
            await _publisher.PublishToGroupAsync("admin:operations", dto, ct: ct);
        }
    }
}
