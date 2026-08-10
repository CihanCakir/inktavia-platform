using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;
using Aizen.Modules.ServiceRequest.Application.Configuration;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.ServiceRequest.Application.Command.Assignment;

[DocumentationInfo("Start assignment command handler", "Starts the assignment and changes SR status to InProgress, publishes WorkStarted event.")]
public sealed class StartServiceRequestAssignmentCommandHandler : AizenCommandHandler<StartServiceRequestAssignmentCommand, StartServiceRequestAssignmentResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IServiceRequestMessageRepository _msgRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly IOptionsMonitor<MessagingWriteCutoverOptions> _cutover;

    public StartServiceRequestAssignmentCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestAssignmentRepository assignmentRepository,
        IServiceRequestMessageRepository msgRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher,
        IOptionsMonitor<MessagingWriteCutoverOptions> cutover)
    {
        _srRepository = srRepository; _assignmentRepository = assignmentRepository;
        _msgRepository = msgRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
        _cutover = cutover;
    }

    public override async Task<StartServiceRequestAssignmentResponse?> Handle(StartServiceRequestAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Ownership guard
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw new AizenBusinessException("Job not found.");

        if (assignment.ProviderProfileId != providerProfileId)
            throw new AizenBusinessException("Job not found.");

        var sr = await _srRepository.GetByIdAsync(assignment.ServiceRequestId, cancellationToken)
            ?? throw new AizenBusinessException("Job not found.");

        // State guard: only startable from Assigned or Scheduled
        if (sr.Status != ServiceRequestStatus.Assigned && sr.Status != ServiceRequestStatus.Scheduled)
            throw new AizenBusinessException("SR_JOB_NOT_STARTABLE");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        assignment.Start();
        _assignmentRepository.Update(assignment);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.InProgress);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.InProgress,
            "Work started", currentUserId, ServiceRequestActorType.Provider);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, assignment.ProviderProfileId,
            ServiceRequestRealtimeEventType.WorkStarted, assignment.ToDto(),
            currentUserId, ServiceRequestActorType.Provider, cancellationToken);

        // BE_WC1 — first-class lifecycle event (ALWAYS published) → Messaging generates the JOB_STARTED System message.
        await _messagePublisher.PublishAsync(new ServiceRequestAssignmentStartedMessage
        {
            ServiceRequestId = sr.Id, RequestCode = sr.RequestCode, AssignmentId = assignment.Id,
            ProviderProfileId = assignment.ProviderProfileId, StartedByUserId = currentUserId,
            OccurredAt = DateTimeOffset.UtcNow,
        }, cancellationToken);

        // Lifecycle system message (idempotent). BE_WC1 flag-gated: when SystemMessages is ON, Messaging owns this
        // (from the event above), so the SR module stops writing the sr.Messages row + the chat-mirror event.
        if (!_cutover.CurrentValue.SystemMessages &&
            !await _msgRepository.HasSystemMessageAsync(sr.Id, "JOB_STARTED", cancellationToken))
        {
            var sysMsg = ServiceRequestMessageEntity.Create(
                sr.Id, currentUserId, ServiceRequestMessageSenderType.System,
                ServiceRequestMessageType.StatusChange, "JOB_STARTED", null);
            await _msgRepository.AddAsync(sysMsg, cancellationToken);

            await _messagePublisher.PublishAsync(new ServiceRequestMessageSentMessage
            {
                ServiceRequestId = sr.Id, MessageId = sysMsg.Id, SenderUserId = currentUserId,
                SenderType = ServiceRequestMessageSenderType.System,
                ProviderProfileId = assignment.ProviderProfileId,
                Content = sysMsg.Content, MessageType = sysMsg.MessageType, OccurredAt = DateTimeOffset.UtcNow
            }, cancellationToken);
        }

        return new StartServiceRequestAssignmentResponse(assignment.Id);
    }
}
