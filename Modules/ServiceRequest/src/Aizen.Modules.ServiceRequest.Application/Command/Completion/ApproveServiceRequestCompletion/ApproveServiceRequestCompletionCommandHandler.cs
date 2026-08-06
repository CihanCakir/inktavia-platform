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
    private readonly IServiceRequestMessageRepository _msgRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;

    public ApproveServiceRequestCompletionCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestCompletionRepository completionRepository,
        IServiceRequestAssignmentRepository assignmentRepository, IServiceRequestMessageRepository msgRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository; _completionRepository = completionRepository;
        _assignmentRepository = assignmentRepository; _msgRepository = msgRepository;
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

        await _messagePublisher.PublishAsync(new ServiceRequestCompletionApprovedMessage
        {
            ServiceRequestId = sr.Id,
            CompletionId = completion.Id,
            ProviderUserId = completion.ProviderUserId,
            OwnerUserId = sr.OwnerUserId
        }, cancellationToken);

        // Lifecycle system message (idempotent)
        if (!await _msgRepository.HasSystemMessageAsync(sr.Id, "JOB_COMPLETED", cancellationToken))
        {
            var sysMsg = ServiceRequestMessageEntity.Create(
                sr.Id, currentUserId, ServiceRequestMessageSenderType.System,
                ServiceRequestMessageType.StatusChange, "JOB_COMPLETED", null);
            await _msgRepository.AddAsync(sysMsg, cancellationToken);

            // Resolve provider profile from assignment
            var assignment = await _assignmentRepository.GetByServiceRequestIdAsync(sr.Id, cancellationToken);
            var providerProfileId = assignment?.ProviderProfileId;

            await _messagePublisher.PublishAsync(new ServiceRequestMessageSentMessage
            {
                ServiceRequestId = sr.Id, MessageId = sysMsg.Id, SenderUserId = currentUserId,
                SenderType = ServiceRequestMessageSenderType.System,
                ProviderProfileId = providerProfileId,
                Content = sysMsg.Content, MessageType = sysMsg.MessageType, OccurredAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        return new ApproveServiceRequestCompletionResponse(completion.Id);
    }
}
