using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Update service request command handler", "Updates the service request profile and publishes realtime event.")]
public sealed class UpdateServiceRequestCommandHandler : AizenCommandHandler<UpdateServiceRequestCommand, UpdateServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public UpdateServiceRequestCommandHandler(IServiceRequestRepository repository, IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _repository = repository; _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<UpdateServiceRequestResponse?> Handle(UpdateServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var req = request.Request;
        entity.UpdateProfile(req.Title, req.Description, req.ServiceCategoryCode, req.ServiceTypeCode,
            req.Priority, req.RequestedStartDate, req.RequestedEndDate,
            req.LocationCountryCode, req.LocationCityCode, req.LocationMarinaName,
            req.LocationLatitude, req.LocationLongitude, req.OwnerNotes, req.ExpiresAt);

        _repository.Update(entity);

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _realtimePublisher.PublishAsync(entity.Id, entity.RequestCode, entity.OwnerUserId, null,
            ServiceRequestRealtimeEventType.ServiceRequestUpdated,
            entity.ToDto(), currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new UpdateServiceRequestResponse(entity.ToDto());
    }
}
