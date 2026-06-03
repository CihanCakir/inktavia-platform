using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Assignment;

[DocumentationInfo("Create assignment command handler", "Creates assignment from accepted offer, sets SR to Assigned, publishes AssignmentCreated.")]
public sealed class CreateServiceRequestAssignmentCommandHandler : AizenCommandHandler<CreateServiceRequestAssignmentCommand, CreateServiceRequestAssignmentResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;

    public CreateServiceRequestAssignmentCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IServiceRequestAssignmentRepository assignmentRepository,
        IAizenInfoAccessor info,
        ServiceRequestRealtimePublisher realtimePublisher)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _assignmentRepository = assignmentRepository; _info = info; _realtimePublisher = realtimePublisher;
    }

    public override async Task<CreateServiceRequestAssignmentResponse?> Handle(CreateServiceRequestAssignmentCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var offer = await _offerRepository.GetByIdAsync(request.Request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException($"Offer {request.Request.OfferId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;

        var assignment = ServiceRequestAssignmentEntity.Create(
            sr.Id, offer.Id, offer.ProviderProfileId, offer.ProviderUserId,
            req.AssignedTeamMemberId, req.ScheduledStartDate, req.ScheduledEndDate);

        await _assignmentRepository.AddAsync(assignment, cancellationToken);

        var prevStatus = sr.Status;
        sr.ChangeStatus(ServiceRequestStatus.Assigned);
        sr.SetAssignment(assignment);

        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Assigned,
            "Assignment created", currentUserId, ServiceRequestActorType.Owner);
        sr.AddStatusHistory(history);
        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, offer.ProviderProfileId,
            ServiceRequestRealtimeEventType.AssignmentCreated, assignment.ToDto(),
            currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new CreateServiceRequestAssignmentResponse(assignment.ToDto());
    }
}
