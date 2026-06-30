using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Completion;

[DocumentationInfo("Reject completion command handler", "Owner rejects completion, transitions SR back to InProgress, publishes CompletionRejected.")]
public sealed class RejectServiceRequestCompletionCommandHandler : AizenCommandHandler<RejectServiceRequestCompletionCommand, RejectServiceRequestCompletionResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestCompletionRepository _completionRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public RejectServiceRequestCompletionCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestCompletionRepository completionRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _completionRepository = completionRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<RejectServiceRequestCompletionResponse?> Handle(RejectServiceRequestCompletionCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var completion = await _completionRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"No completion found for ServiceRequest {request.ServiceRequestId}.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        completion.RejectByOwner(currentUserId, request.Request.ReviewNotes);
        _completionRepository.Update(completion);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.InProgress);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.InProgress,
            "Completion rejected", currentUserId, ServiceRequestActorType.Owner);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.CompletionRejected, completion.ToDto(),
            currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new RejectServiceRequestCompletionResponse(completion.Id);
    }
}
