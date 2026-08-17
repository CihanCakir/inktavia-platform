using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Provider.GetMyOffers;

[DocumentationInfo("Get my offers handler", "Returns all offers by the calling provider with SR snapshot, scoped by asserted profile id.")]
public sealed class GetMyOffersQueryHandler
    : AizenQueryHandler<GetMyOffersQuery, GetMyOffersResponse>
{
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceRequestRepository _srRepository;
    private readonly IAizenInfoAccessor _info;

    public GetMyOffersQueryHandler(
        IServiceRequestOfferRepository offerRepository,
        IServiceRequestRepository srRepository,
        IAizenInfoAccessor info)
    {
        _offerRepository = offerRepository;
        _srRepository = srRepository;
        _info = info;
    }

    public override async Task<GetMyOffersResponse?> Handle(GetMyOffersQuery request, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            return new GetMyOffersResponse { PageIndex = request.PageIndex, PageSize = request.PageSize };

        var skip = request.PageIndex * request.PageSize;
        var offers = await _offerRepository.GetByProviderProfileIdWithSrAsync(
            providerProfileId, request.StatusFilter, skip, request.PageSize, ct);

        var srIds = offers.Select(o => o.ServiceRequestId).Distinct().ToList();
        var srLookup = new Dictionary<long, (string Title, string Code)>();
        foreach (var srId in srIds)
        {
            var sr = await _srRepository.GetByIdAsync(srId, ct);
            if (sr is not null)
                srLookup[srId] = (sr.Title, sr.RequestCode);
        }

        return new GetMyOffersResponse
        {
            Items = offers.Select(o =>
            {
                srLookup.TryGetValue(o.ServiceRequestId, out var sr);
                return new MyOfferItemDto
                {
                    OfferId = o.Id,
                    ServiceRequestId = o.ServiceRequestId,
                    ServiceRequestTitle = sr.Title ?? "",
                    ServiceRequestCode = sr.Code ?? "",
                    OfferStatus = o.Status.ToString(),
                    TotalAmount = o.TotalAmount,
                    CurrencyCode = o.CurrencyCode,
                    EstimatedStartDate = o.EstimatedStartDate,
                    EstimatedEndDate = o.EstimatedEndDate,
                    CreatedAt = o.CreateDate,
                    AcceptedAt = o.AcceptedAt,
                    RejectedAt = o.RejectedAt,
                    WithdrawnAt = o.WithdrawnAt,
                };
            }).ToList(),
            PageIndex = request.PageIndex,
            PageSize = request.PageSize,
        };
    }
}
