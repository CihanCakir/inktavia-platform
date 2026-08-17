using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Create service request command handler", "Creates a new service request, adds initial items if provided, records initial status history, and publishes realtime event.")]
public sealed class CreateServiceRequestCommandHandler : AizenCommandHandler<CreateServiceRequestCommand, CreateServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IServiceRequestReferenceDataRemoteCall _referenceData;

    public CreateServiceRequestCommandHandler(
        IServiceRequestRepository repository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher,
        IServiceRequestReferenceDataRemoteCall referenceData)
    {
        _repository = repository;
        _info = info;
        _realtimePublisher = realtimePublisher;
        _referenceData = referenceData;
    }

    public override async Task<CreateServiceRequestResponse?> Handle(CreateServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;
        var requestCode = $"SR{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var entity = ServiceRequestEntity.Create(
            requestCode, currentUserId, req.VesselId, req.ServiceCategoryCode, req.ServiceTypeCode,
            req.Title, req.Description, req.Priority, req.RequestedStartDate, req.RequestedEndDate,
            req.LocationCountryCode, req.LocationCityCode, req.LocationMarinaName,
            req.LocationLatitude, req.LocationLongitude, req.OwnerNotes, req.ExpiresAt);

        if (!string.IsNullOrWhiteSpace(req.LocationCityCode))
        {
            var countryCode = req.LocationCountryCode ?? "TR";
            var cityResult = await _referenceData.GetCity(countryCode, req.LocationCityCode);
            if (cityResult.Body is null || !cityResult.Body.IsActive)
                throw new AizenBusinessException($"Location city code '{req.LocationCityCode}' is not a recognised ReferenceData city.");
        }

        await _repository.AddAsync(entity, cancellationToken);

        int sortOrder = 0;
        foreach (var item in req.Items)
        {
            var itemEntity = ServiceRequestItemEntity.Create(
                entity.Id, item.ItemType, item.Title, item.Description,
                item.Quantity, item.UnitCode, item.EstimatedUnitPrice, sortOrder++);
            entity.AddItem(itemEntity);
        }

        var history = ServiceRequestStatusHistoryEntity.Create(
            entity.Id, ServiceRequestStatus.Draft, ServiceRequestStatus.Draft,
            "Created", currentUserId, ServiceRequestActorType.Owner);
        entity.AddStatusHistory(history);

        await _realtimePublisher.PublishAsync(
            entity.Id, entity.RequestCode, currentUserId, null,
            ServiceRequestRealtimeEventType.ServiceRequestCreated,
            entity.ToDto(), currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new CreateServiceRequestResponse(entity.ToDto());
    }
}
