using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;

[DocumentationInfo("Get service request list query handler", "Returns paginated service request summaries for an owner.")]
public sealed class GetServiceRequestListQueryHandler : AizenQueryHandler<GetServiceRequestListQuery, GetServiceRequestListResponse>
{
    private readonly IServiceRequestRepository _repository;

    public GetServiceRequestListQueryHandler(IServiceRequestRepository repository) => _repository = repository;

    public override async Task<GetServiceRequestListResponse> Handle(GetServiceRequestListQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var skip = filter.PageIndex * filter.PageSize;

        var items = await _repository.GetByOwnerUserIdAsync(request.OwnerUserId, skip, filter.PageSize, cancellationToken);
        var total = await _repository.CountByOwnerUserIdAsync(request.OwnerUserId, cancellationToken);

        var dtos = items.Select(e => e.ToSummaryDto()).ToList();
        return new GetServiceRequestListResponse(dtos, total);
    }
}
