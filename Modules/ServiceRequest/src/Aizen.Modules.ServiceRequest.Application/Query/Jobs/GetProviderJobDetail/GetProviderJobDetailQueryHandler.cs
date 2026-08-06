using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

/// <summary>
/// Job detail aggregate: assignment + SR (title/code/workscope/location/timeline) + accepted offer (items + totals).
/// Access-checked: provider must own the assignment. In-module joins — no cross-module call.
/// </summary>
public sealed class GetProviderJobDetailQueryHandler
    : AizenQueryHandler<GetProviderJobDetailQuery, GetProviderJobDetailResponse>
{
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;

    public GetProviderJobDetailQueryHandler(
        IServiceRequestAssignmentRepository assignmentRepository,
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info)
    {
        _assignmentRepository = assignmentRepository;
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _info = info;
    }

    public override async Task<GetProviderJobDetailResponse?> Handle(
        GetProviderJobDetailQuery request, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, ct)
            ?? throw new AizenBusinessException("Job not found.");

        if (assignment.ProviderProfileId != profileId)
            throw new AizenBusinessException("Job not found.");

        var sr = await _srRepository.GetByIdWithDetailsAsync(assignment.ServiceRequestId, ct)
            ?? throw new AizenBusinessException("Job not found.");

        var offer = await _offerRepository.GetByIdAsync(assignment.ServiceRequestOfferId, ct);

        var detail = sr.ToProviderDetailDto(profileId);

        // DD-2: the job aggregate is served only to the assigned provider (checked above) — give the exact
        // berth coordinates, not the discovery-snapped point, so the map + directions land on the vessel.
        detail.Request.ApproxLatitude = sr.LocationLatitude;
        detail.Request.ApproxLongitude = sr.LocationLongitude;

        return new GetProviderJobDetailResponse
        {
            AssignmentId = assignment.Id,
            ServiceRequestId = assignment.ServiceRequestId,
            ServiceRequestOfferId = assignment.ServiceRequestOfferId,
            Status = sr.Status.ToString(),
            AssignmentStatus = assignment.Status.ToString(),
            ScheduledStartDate = assignment.ScheduledStartDate,
            ScheduledEndDate = assignment.ScheduledEndDate,
            ActualStartDate = assignment.ActualStartDate,
            ActualEndDate = assignment.ActualEndDate,
            ProviderNotes = assignment.ProviderNotes,

            Request = detail.Request,
            WorkScope = detail.WorkScope,
            Attachments = detail.Attachments,
            Timeline = detail.Timeline,

            AcceptedOffer = offer?.ToDto(),

            CompletionEvidenceFileId = sr.Completion?.EvidenceFileId,
            CompletedAtUtc = sr.Completion?.SubmittedAt,
            AutoApproveAt = sr.Completion?.AutoApproveAt
        };
    }
}
