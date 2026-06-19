using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin;

[DocumentationInfo("Admin service request list query handler", "Returns paginated service request summaries for admin view.")]
public sealed class GetAdminServiceRequestListQueryHandler : AizenQueryHandler<GetAdminServiceRequestListQuery, GetAdminServiceRequestListResponse>
{
    private readonly IServiceRequestRepository _repository;

    public GetAdminServiceRequestListQueryHandler(IServiceRequestRepository repository) => _repository = repository;

    public override async Task<GetAdminServiceRequestListResponse> Handle(GetAdminServiceRequestListQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;

        var items = await _repository.GetAdminListAsync(filter, cancellationToken);
        var total = await _repository.CountAdminAsync(filter, cancellationToken);

        var dtos = items.Select(e => e.ToSummaryDto()).ToList();
        return new GetAdminServiceRequestListResponse(dtos, total);
    }
}
