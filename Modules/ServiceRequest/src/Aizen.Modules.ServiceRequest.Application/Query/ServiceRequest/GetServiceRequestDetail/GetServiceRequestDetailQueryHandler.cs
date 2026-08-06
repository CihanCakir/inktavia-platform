using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.ServiceRequest;

[DocumentationInfo("Get service request detail query handler", "Fetches a service request with all related data and maps to detail DTO.")]
public sealed class GetServiceRequestDetailQueryHandler : AizenQueryHandler<GetServiceRequestDetailQuery, GetServiceRequestDetailResponse>
{
    private readonly IServiceRequestRepository _repository;

    public GetServiceRequestDetailQueryHandler(IServiceRequestRepository repository) => _repository = repository;

    public override async Task<GetServiceRequestDetailResponse> Handle(GetServiceRequestDetailQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdWithDetailsAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");

        var detail = entity.ToDetailDto();

        // BE-S4 — enrich Travel offer lines with their structured travel-pricing detail (descriptive; drives the admin
        // read-only Travel block). Travel is an Exempt pass-through, so the resolved amount is the line's own LineTotal.
        var travelItemIds = detail.Offers
            .SelectMany(o => o.Items)
            .Where(i => i.ItemType == ServiceRequestOfferItemType.Travel)
            .Select(i => i.Id)
            .ToList();

        if (travelItemIds.Count > 0)
        {
            var travelByItem = await _repository.GetTravelPricingByOfferItemIdsAsync(travelItemIds, cancellationToken);
            foreach (var item in detail.Offers.SelectMany(o => o.Items))
            {
                if (item.ItemType == ServiceRequestOfferItemType.Travel &&
                    travelByItem.TryGetValue(item.Id, out var t))
                {
                    item.Travel = new TravelPricingDetailDto
                    {
                        OfferItemId         = t.OfferItemId,
                        Method              = t.Method,
                        OriginCityCode      = t.OriginCityCode,
                        DestinationCityCode = t.DestinationCityCode,
                        DistanceKm          = t.DistanceKm,
                        PerKmRate           = t.PerKmRate,
                        UnitCode            = t.UnitCode,
                    };
                }
            }
        }

        return new GetServiceRequestDetailResponse(detail);
    }
}
