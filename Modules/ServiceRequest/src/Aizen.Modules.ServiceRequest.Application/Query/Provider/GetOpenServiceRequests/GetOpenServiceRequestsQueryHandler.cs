using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetOpenServiceRequests;

[DocumentationInfo("Get open service requests handler", "Returns biddable SRs for the calling provider, scoped by asserted profile id.")]
public sealed class GetOpenServiceRequestsQueryHandler
    : AizenQueryHandler<GetOpenServiceRequestsQuery, GetOpenServiceRequestsResponse>
{
    private readonly IServiceRequestRepository _repository;
    private readonly IAizenInfoAccessor _info;

    public GetOpenServiceRequestsQueryHandler(IServiceRequestRepository repository, IAizenInfoAccessor info)
    {
        _repository = repository;
        _info = info;
    }

    public override async Task<GetOpenServiceRequestsResponse?> Handle(
        GetOpenServiceRequestsQuery request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            return new GetOpenServiceRequestsResponse { PageIndex = request.PageIndex, PageSize = request.PageSize };

        var filter = new ProviderAvailableServiceRequestFilterRequest
        {
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            ServiceCategoryCode = request.ServiceCategoryCode,
            LocationCityCode = request.LocationCityCode,
            LocationCountryCode = request.LocationCountryCode,
            MinPriority = request.MinPriority,
            SearchTerm = request.SearchTerm,
        };

        var items = await _repository.GetOpenForProviderAsync(providerProfileId, filter, ct);
        var totalCount = await _repository.CountOpenForProviderAsync(providerProfileId, filter, ct);

        return new GetOpenServiceRequestsResponse
        {
            Items = items.Select(sr => new OpenServiceRequestItemDto
            {
                Id = sr.Id,
                RequestCode = sr.RequestCode,
                Title = sr.Title,
                ServiceCategoryCode = sr.ServiceCategoryCode,
                LocationCityCode = sr.LocationCityCode,
                LocationCountryCode = sr.LocationCountryCode,
                LocationMarinaName = sr.LocationMarinaName,
                Priority = sr.Priority.ToString(),
                RequestedStartDate = sr.RequestedStartDate,
                RequestedEndDate = sr.RequestedEndDate,
                AttachmentCount = sr.Attachments.Count,
                OfferCount = sr.Offers.Count,
                CreatedAt = sr.CreateDate,
            }).ToList(),
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
            TotalCount = totalCount,
        };
    }
}
