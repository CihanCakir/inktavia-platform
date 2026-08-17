using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin;

[DocumentationInfo("Admin service request status breakdown query handler",
    "Maps the domain (status → count) read-model to lowercase enum keys the BFF/FE use (WaitingForOffer → waitingforoffer).")]
public sealed class GetAdminServiceRequestStatusBreakdownQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestStatusBreakdownQuery, List<ServiceRequestStatusCountDto>>
{
    private readonly IServiceRequestRepository _repository;

    public GetAdminServiceRequestStatusBreakdownQueryHandler(IServiceRequestRepository repository)
        => _repository = repository;

    public override async Task<List<ServiceRequestStatusCountDto>> Handle(
        GetAdminServiceRequestStatusBreakdownQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.GetStatusBreakdownAsync(cancellationToken);
        return rows
            .Select(r => new ServiceRequestStatusCountDto
            {
                Status = r.Status.ToString().ToLowerInvariant(),
                Count = r.Count,
            })
            .ToList();
    }
}
