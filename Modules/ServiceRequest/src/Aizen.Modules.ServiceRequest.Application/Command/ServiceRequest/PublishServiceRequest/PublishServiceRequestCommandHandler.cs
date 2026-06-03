using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Publish service request command handler", "Transitions a draft service request to Open and publishes StatusChanged realtime event.")]
public sealed class PublishServiceRequestCommandHandler : AizenCommandHandler<PublishServiceRequestCommand, UpdateServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public PublishServiceRequestCommandHandler(
        IServiceRequestRepository repository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _repository = repository;
        _info = info;
        _realtimePublisher = realtimePublisher;
    }

    public override async Task<UpdateServiceRequestResponse?> Handle(PublishServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        entity.Publish();
        _repository.Update(entity);

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        await _realtimePublisher.PublishAsync(entity.Id, entity.RequestCode, entity.OwnerUserId, null,
            ServiceRequestRealtimeEventType.ServiceRequestStatusChanged,
            entity.ToDto(), currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new UpdateServiceRequestResponse(entity.ToDto());
    }
}
