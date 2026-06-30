using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
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
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public StartServiceRequestAssignmentCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestAssignmentRepository assignmentRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _assignmentRepository = assignmentRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<StartServiceRequestAssignmentResponse?> Handle(StartServiceRequestAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Assignment {request.AssignmentId} not found.");
        var sr = await _srRepository.GetByIdAsync(assignment.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {assignment.ServiceRequestId} not found.");

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

        return new StartServiceRequestAssignmentResponse(assignment.Id);
    }
}
