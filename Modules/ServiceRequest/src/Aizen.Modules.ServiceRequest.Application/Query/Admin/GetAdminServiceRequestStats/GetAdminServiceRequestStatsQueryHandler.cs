using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin;

[DocumentationInfo("Admin service request stats query handler", "Returns real totalRequests / activeRepairs / criticalAlerts computed over the whole dataset.")]
public sealed class GetAdminServiceRequestStatsQueryHandler
    : AizenQueryHandler<GetAdminServiceRequestStatsQuery, GetAdminServiceRequestStatsResponse>
{
    private readonly IServiceRequestRepository _repository;

    public GetAdminServiceRequestStatsQueryHandler(IServiceRequestRepository repository) => _repository = repository;

    public override Task<GetAdminServiceRequestStatsResponse> Handle(
        GetAdminServiceRequestStatsQuery request, CancellationToken cancellationToken)
        => _repository.GetAdminStatsAsync(cancellationToken);
}
