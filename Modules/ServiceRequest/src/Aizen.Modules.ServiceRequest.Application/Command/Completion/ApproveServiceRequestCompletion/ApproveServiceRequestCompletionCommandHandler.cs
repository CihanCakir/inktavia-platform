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
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IAizenMessagePublisher _messagePublisher;

    public ApproveServiceRequestCompletionCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestCompletionRepository completionRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IAizenMessagePublisher messagePublisher)
    {
        _srRepository = srRepository; _completionRepository = completionRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _messagePublisher = messagePublisher;
    }

    public override async Task<ApproveServiceRequestCompletionResponse?> Handle(ApproveServiceRequestCompletionCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var completion = await _completionRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"No completion found for ServiceRequest {request.ServiceRequestId}.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        completion.ApproveByOwner(currentUserId, request.Request.ReviewNotes);
        _completionRepository.Update(completion);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.Completed);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Completed,
            "Completion approved", currentUserId, ServiceRequestActorType.Owner);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, null,
            ServiceRequestRealtimeEventType.CompletionApproved, completion.ToDto(),
            currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        await _messagePublisher.PublishAsync(new ServiceRequestCompletionApprovedMessage
        {
            ServiceRequestId = sr.Id,
            CompletionId = completion.Id,
            ProviderUserId = completion.ProviderUserId,
            OwnerUserId = currentUserId
        }, cancellationToken);

        return new ApproveServiceRequestCompletionResponse(completion.Id);
    }
}
