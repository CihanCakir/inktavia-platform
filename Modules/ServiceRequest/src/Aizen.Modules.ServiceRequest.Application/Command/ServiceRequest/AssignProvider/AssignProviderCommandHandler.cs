using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Assign provider command handler", "Sets the assigned provider on a service request and transitions status to Assigned.")]
public sealed class AssignProviderCommandHandler : AizenCommandHandler<AssignProviderCommand, AssignProviderResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public AssignProviderCommandHandler(
        IServiceRequestRepository repository, IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _repository = repository; _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<AssignProviderResponse?> Handle(AssignProviderCommand request, CancellationToken cancellationToken)
    {
        var sr = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var prevStatus = sr.Status;

        sr.Assign(request.Request.ProviderId, request.Request.ProviderName);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Assigned,
            $"Assigned to provider {request.Request.ProviderName}", currentUserId, ServiceRequestActorType.Admin);
        sr.AddStatusHistory(history);
        _repository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.AssignmentCreated,
            new { ServiceRequestId = sr.Id, ProviderId = request.Request.ProviderId, ProviderName = request.Request.ProviderName },
            currentUserId, ServiceRequestActorType.Admin, cancellationToken);

        return new AssignProviderResponse(sr.Id, request.Request.ProviderId);
    }
}
