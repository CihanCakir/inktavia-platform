using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.Admin;

[DocumentationInfo("Admin dispute list query handler", "Returns paginated open disputes for admin view.")]
public sealed class GetAdminDisputeListQueryHandler : AizenQueryHandler<GetAdminDisputeListQuery, GetAdminDisputeListResponse>
{
    private readonly IServiceRequestDisputeRepository _repository;

    public GetAdminDisputeListQueryHandler(IServiceRequestDisputeRepository repository) => _repository = repository;

    public override async Task<GetAdminDisputeListResponse> Handle(GetAdminDisputeListQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var skip = filter.PageIndex * filter.PageSize;
        var disputes = await _repository.GetAllOpenAsync(skip, filter.PageSize, cancellationToken);
        return new GetAdminDisputeListResponse(disputes.Select(d => d.ToDto()).ToList(), disputes.Count);
    }
}
