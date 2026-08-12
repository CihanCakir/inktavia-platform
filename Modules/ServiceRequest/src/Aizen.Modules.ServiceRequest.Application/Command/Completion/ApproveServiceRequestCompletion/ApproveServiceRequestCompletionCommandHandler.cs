using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Completion;

[DocumentationInfo("Approve completion command handler", "Owner approves completion, transitions SR to Completed, publishes CompletionApproved.")]
public sealed class ApproveServiceRequestCompletionCommandHandler : AizenCommandHandler<ApproveServiceRequestCompletionCommand, ApproveServiceRequestCompletionResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestCompletionRepository _completionRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;

    public ApproveServiceRequestCompletionCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestCompletionRepository completionRepository,
        IServiceRequestAssignmentRepository assignmentRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository; _completionRepository = completionRepository;
        _assignmentRepository = assignmentRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
    }

    public override async Task<ApproveServiceRequestCompletionResponse?> Handle(ApproveServiceRequestCompletionCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var completion = await _completionRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"No completion found for ServiceRequest {request.ServiceRequestId}.");

        // N3-C — the acting user is the JWT user on the owner path, or the configured system id on the auto-approval job.
        var currentUserId = request.ActingUserIdOverride ?? _info.UserInfoAccessor.UserInfo.UserId;
        var actorType     = request.ActorTypeOverride ?? ServiceRequestActorType.Owner;

        // N3-C idempotency: only a still-pending (Submitted) completion is approvable. If the owner already
        // approved/rejected/disputed (manual action before the deadline, or a duplicate/racy call), no-op — so the
        // auto-approval job never double-approves and never re-publishes the approval event.
        if (completion.Status != ServiceRequestCompletionStatus.Submitted)
            return new ApproveServiceRequestCompletionResponse(completion.Id);

        completion.ApproveByOwner(currentUserId, request.Request.ReviewNotes);
        // MO4 — optional owner satisfaction rating captured at approval (1..5). Additive: it never affects the
        // SR→Completed transition or the decoupled escrow release; the auto-approval job passes no rating.
        if (request.Request.ClientRating is int rating)
            completion.RateByClient(rating);
        _completionRepository.Update(completion);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.Completed);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Completed,
            "Completion approved", currentUserId, actorType);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.CompletionApproved, completion.ToDto(),
            currentUserId, actorType, cancellationToken);

        // BE_NF1 (D5) — carry the provider's PROFILE id so Notification files the CompletionApproved notification where
        // provider inbox/tokens/prefs live (keyed by ProviderProfileId, like OfferCreated/AssignmentCreated), not the
        // raw ProviderUserId. Resolve from the SR's assignment.
        var assignment = await _assignmentRepository.GetByServiceRequestIdAsync(sr.Id, cancellationToken);

        await _messagePublisher.PublishAsync(new ServiceRequestCompletionApprovedMessage
        {
            ServiceRequestId = sr.Id,
            CompletionId = completion.Id,
            ProviderUserId = completion.ProviderUserId,
            ProviderProfileId = assignment?.ProviderProfileId ?? 0,
            OwnerUserId = sr.OwnerUserId
        }, cancellationToken);

        // BE_WC4b — the JOB_COMPLETED System message is produced solely by the Messaging WC1 lifecycle consumer (from
        // the ServiceRequestCompletionApprovedMessage above). The SR module no longer writes sr.Messages.

        return new ApproveServiceRequestCompletionResponse(completion.Id);
    }
}
