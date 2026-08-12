using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Assignment;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Assignment;

[DocumentationInfo("Create assignment command handler", "Creates assignment from accepted offer, sets SR to Assigned, publishes AssignmentCreated.")]
public sealed class CreateServiceRequestAssignmentCommandHandler : AizenCommandHandler<CreateServiceRequestAssignmentCommand, CreateServiceRequestAssignmentResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestAssignmentCreator _assignmentCreator;

    public CreateServiceRequestAssignmentCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info,
        ServiceRequestAssignmentCreator assignmentCreator)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _info = info;
        _assignmentCreator = assignmentCreator;
    }

    public override async Task<CreateServiceRequestAssignmentResponse?> Handle(CreateServiceRequestAssignmentCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var offer = await _offerRepository.GetByIdAsync(request.Request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException($"Offer {request.Request.OfferId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var req = request.Request;

        // FIX_ASSIGNMENT_ON_ACCEPT — the single shared create path (idempotent). This manual endpoint forwards its
        // schedule; the auto-create on owner accept passes none.
        var result = await _assignmentCreator.CreateFromAcceptedOfferAsync(
            sr, offer, currentUserId, req.AssignedTeamMemberId, req.ScheduledStartDate, req.ScheduledEndDate,
            cancellationToken);

        return new CreateServiceRequestAssignmentResponse(result.Assignment.ToDto());
    }
}
