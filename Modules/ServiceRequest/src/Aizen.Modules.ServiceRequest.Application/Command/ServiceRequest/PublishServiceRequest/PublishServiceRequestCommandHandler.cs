using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Publish service request command handler", "Transitions a draft service request to Open, pushes the realtime event, and announces it on the bus so providers can be told.")]
public sealed class PublishServiceRequestCommandHandler : AizenCommandHandler<PublishServiceRequestCommand, UpdateServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;

    public PublishServiceRequestCommandHandler(
        IServiceRequestRepository repository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher,
        IServiceRequestReferenceDataRemoteCall referenceData)
    {
        _repository = repository;
        _info = info;
        _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
        _referenceData = referenceData;
    }

    public override async Task<UpdateServiceRequestResponse?> Handle(PublishServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        if (!string.IsNullOrWhiteSpace(entity.LocationCityCode))
        {
            var countryCode = entity.LocationCountryCode ?? "TR";
            var cityResult = await _referenceData.GetCity(countryCode, entity.LocationCityCode);
            if (cityResult.Body is null || !cityResult.Body.IsActive)
                throw new AizenBusinessException($"Location city code '{entity.LocationCityCode}' is not a recognised ReferenceData city.");
        }

        entity.Publish();
        _repository.Update(entity);

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _realtimePublisher.PublishAsync(entity.Id, entity.RequestCode, entity.OwnerUserId, null,
            ServiceRequestRealtimeEventType.ServiceRequestStatusChanged,
            entity.ToDto(), currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        // The realtime publisher only reaches THIS process's own SignalR hub — nothing ever left the module. That is
        // why every ServiceRequest consumer elsewhere (Notification's, and now the provider BFF's) had nothing to
        // consume: the eight bus contracts existed and were never published by anyone. Announce it.
        await _messagePublisher.PublishAsync(new ServiceRequestPublishedMessage
        {
            ServiceRequestId = entity.Id,
            RequestCode = entity.RequestCode,
            Title = entity.Title,
            OwnerUserId = entity.OwnerUserId,
            ServiceCategoryCode = entity.ServiceCategoryCode,
            Priority = entity.Priority,
            LocationCityCode = entity.LocationCityCode,
            LocationCountryCode = entity.LocationCountryCode,
            LocationMarinaName = entity.LocationMarinaName,
            RequestedStartDate = entity.RequestedStartDate,
        }, cancellationToken);

        return new UpdateServiceRequestResponse(entity.ToDto());
    }
}
