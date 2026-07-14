using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Completion;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Completion;

[DocumentationInfo("Submit completion command handler", "Persists completion submission and transitions SR to CompletionSubmitted.")]
public sealed class SubmitServiceRequestCompletionCommandHandler : AizenCommandHandler<SubmitServiceRequestCompletionCommand, SubmitServiceRequestCompletionResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IServiceRequestCompletionRepository _completionRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;

    public SubmitServiceRequestCompletionCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestAssignmentRepository assignmentRepository,
        IServiceRequestCompletionRepository completionRepository, IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher, IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository; _assignmentRepository = assignmentRepository;
        _completionRepository = completionRepository; _info = info; _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
    }

    public override async Task<SubmitServiceRequestCompletionResponse?> Handle(SubmitServiceRequestCompletionCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Assignment {request.AssignmentId} not found.");
        var sr = await _srRepository.GetByIdAsync(assignment.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {assignment.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;

        var completion = ServiceRequestCompletionEntity.Create(
            sr.Id, assignment.Id, currentUserId, req.CompletionNotes, req.EvidenceFileId);

        await _completionRepository.AddAsync(completion, cancellationToken);

        assignment.Complete();
        _assignmentRepository.Update(assignment);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.CompletionSubmitted);
        sr.SetCompletion(completion);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.CompletionSubmitted,
            "Completion submitted", currentUserId, ServiceRequestActorType.Provider);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, assignment.ProviderProfileId,
            ServiceRequestRealtimeEventType.CompletionSubmitted, completion.ToDto(),
            currentUserId, ServiceRequestActorType.Provider, cancellationToken);

        await _messagePublisher.PublishAsync(new ServiceRequestCompletionSubmittedMessage
        {
            ServiceRequestId = sr.Id,
            CompletionId = completion.Id,
            OwnerUserId = sr.OwnerUserId,
            ProviderUserId = currentUserId
        }, cancellationToken);

        return new SubmitServiceRequestCompletionResponse(completion.ToDto());
    }
}
