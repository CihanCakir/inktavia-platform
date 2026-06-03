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
        var skip = filter.PageIndex * filter.PageSize;

        IReadOnlyList<Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest.ServiceRequestEntity> items;
        int total;

        if (filter.OwnerUserId.HasValue)
        {
            items = await _repository.GetByOwnerUserIdAsync(filter.OwnerUserId.Value, skip, filter.PageSize, cancellationToken);
            total = await _repository.CountByOwnerUserIdAsync(filter.OwnerUserId.Value, cancellationToken);
        }
        else
        {
            items = Array.Empty<Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest.ServiceRequestEntity>();
            total = 0;
        }

        var dtos = items.Select(e => e.ToSummaryDto()).ToList();
        return new GetAdminServiceRequestListResponse(dtos, total);
    }
}
