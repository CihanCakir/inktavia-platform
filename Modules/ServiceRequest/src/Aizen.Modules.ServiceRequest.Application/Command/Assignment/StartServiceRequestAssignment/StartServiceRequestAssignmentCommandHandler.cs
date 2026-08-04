using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

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

    public StartServiceRequestAssignmentCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestAssignmentRepository assignmentRepository,
        IServiceRequestMessageRepository msgRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository; _assignmentRepository = assignmentRepository;
        _msgRepository = msgRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
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

        // Lifecycle system message (idempotent)
        if (!await _msgRepository.HasSystemMessageAsync(sr.Id, "JOB_STARTED", cancellationToken))
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
