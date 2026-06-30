using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest;

[DocumentationInfo("Complete service request command handler", "Sets status to Completed and publishes realtime event.")]
public sealed class CompleteServiceRequestCommandHandler : AizenCommandHandler<CompleteServiceRequestCommand, CompleteServiceRequestResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public CompleteServiceRequestCommandHandler(
        IServiceRequestRepository repository, IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _repository = repository; _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<CompleteServiceRequestResponse?> Handle(CompleteServiceRequestCommand request, CancellationToken cancellationToken)
    {
        var sr = await _repository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var prevStatus = sr.Status;

        sr.MarkCompleted(DateTimeOffset.UtcNow);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Completed,
            "Marked as completed", currentUserId, ServiceRequestActorType.Admin);
        sr.AddStatusHistory(history);
        _repository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, sr.Assignment?.ProviderProfileId,
            ServiceRequestRealtimeEventType.CompletionApproved,
            new { ServiceRequestId = sr.Id },
            currentUserId, ServiceRequestActorType.Admin, cancellationToken);

        return new CompleteServiceRequestResponse(sr.Id);
    }
}
