using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Assignment;

[DocumentationInfo("Accept assignment command handler", "Provider accepts the assignment and publishes AssignmentAccepted event.")]
public sealed class AcceptServiceRequestAssignmentCommandHandler : AizenCommandHandler<AcceptServiceRequestAssignmentCommand, AcceptServiceRequestAssignmentResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public AcceptServiceRequestAssignmentCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestAssignmentRepository assignmentRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _assignmentRepository = assignmentRepository;
        _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<AcceptServiceRequestAssignmentResponse?> Handle(AcceptServiceRequestAssignmentCommand request, CancellationToken cancellationToken)
    {
        var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken)
            ?? throw new InvalidOperationException($"Assignment {request.AssignmentId} not found.");
        var sr = await _srRepository.GetByIdAsync(assignment.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {assignment.ServiceRequestId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        assignment.Accept();
        _assignmentRepository.Update(assignment);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, assignment.ProviderProfileId,
            ServiceRequestRealtimeEventType.AssignmentAccepted, assignment.ToDto(),
            currentUserId, ServiceRequestActorType.Provider, cancellationToken);

        return new AcceptServiceRequestAssignmentResponse(assignment.Id);
    }
}
