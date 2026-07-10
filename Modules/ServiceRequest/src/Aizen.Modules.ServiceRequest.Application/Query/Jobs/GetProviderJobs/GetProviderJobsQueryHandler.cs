using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Jobs;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Jobs;

/// <summary>
/// Returns the caller provider's own assignments. The provider profile id is read from the trusted request
/// context (asserted by the BFF via X-Aizen-Provider-Profile-Id → KeycloakTokenInfo.ProviderProfileId), never
/// from a request parameter. Returns an empty list when no provider profile is present in the context.
/// </summary>
[DocumentationInfo("Get provider jobs query handler", "Lists provider assignments scoped by the asserted provider profile id.")]
public sealed class GetProviderJobsQueryHandler : AizenQueryHandler<GetProviderJobsQuery, GetProviderJobsResponse>
{
    private readonly IServiceRequestAssignmentRepository _assignmentRepository;
    private readonly IAizenInfoAccessor _info;

    public GetProviderJobsQueryHandler(
        IServiceRequestAssignmentRepository assignmentRepository,
        IAizenInfoAccessor info)
    {
        _assignmentRepository = assignmentRepository;
        _info = info;
    }

    public override async Task<GetProviderJobsResponse?> Handle(GetProviderJobsQuery request, CancellationToken cancellationToken)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;

        if (providerProfileId <= 0)
            return new GetProviderJobsResponse { PageIndex = request.PageIndex, PageSize = request.PageSize };

        var skip = request.PageIndex * request.PageSize;
        var assignments = await _assignmentRepository
            .GetByProviderProfileIdAsync(providerProfileId, skip, request.PageSize, cancellationToken);

        var items = assignments
            .Select(a => new ProviderJobItemDto
            {
                AssignmentId = a.Id,
                ServiceRequestId = a.ServiceRequestId,
                ServiceRequestOfferId = a.ServiceRequestOfferId,
                Status = a.Status.ToString(),
                ScheduledStartDate = a.ScheduledStartDate,
                ScheduledEndDate = a.ScheduledEndDate,
                ActualStartDate = a.ActualStartDate,
                ActualEndDate = a.ActualEndDate,
                ProviderNotes = a.ProviderNotes
            })
            .ToList();

        return new GetProviderJobsResponse
        {
            Items = items,
            PageIndex = request.PageIndex,
            PageSize = request.PageSize
        };
    }
}
