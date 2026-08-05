using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Travel;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Services.Travel;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Command.Travel;

/// <summary>BE-S4a — upsert the travel-pricing detail on a provider-owned, still-open offer's Travel line.</summary>
public sealed class SetOfferLineTravelPricingCommandHandler
    : AizenCommandHandler<SetOfferLineTravelPricingCommand, TravelPricingDetailDto>
{
    private readonly ServiceRequestDbContext _db;
    private readonly IAizenInfoAccessor _info;
    private readonly TravelPricingValidator _validator;

    public SetOfferLineTravelPricingCommandHandler(
        ServiceRequestDbContext db, IAizenInfoAccessor info, TravelPricingValidator validator)
    { _db = db; _info = info; _validator = validator; }

    public override async Task<TravelPricingDetailDto?> Handle(SetOfferLineTravelPricingCommand command, CancellationToken ct)
    {
        var profileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (profileId <= 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var offer = await _db.ServiceRequestOffers
            .FirstOrDefaultAsync(o => o.Id == command.OfferId && o.ProviderProfileId == profileId && !o.IsDeleted, ct)
            ?? throw new AizenBusinessException("SR_TRAVEL_OFFER_NOT_FOUND");

        if (offer.Status is ServiceRequestOfferStatus.Accepted or ServiceRequestOfferStatus.Rejected
            or ServiceRequestOfferStatus.Withdrawn or ServiceRequestOfferStatus.Expired)
            throw new AizenBusinessException($"SR_TRAVEL_OFFER_LOCKED: offer is {offer.Status}");

        var item = await _db.ServiceRequestOfferItems
            .FirstOrDefaultAsync(i => i.Id == command.OfferItemId && i.ServiceRequestOfferId == command.OfferId && !i.IsDeleted, ct)
            ?? throw new AizenBusinessException("SR_TRAVEL_LINE_NOT_FOUND");

        var req = command.Request ?? throw new AizenBusinessException("SR_TRAVEL_REQUEST_REQUIRED");
        await _validator.ValidateAsync(item, req, ct);

        // Upsert — at most one detail per line.
        var existing = await _db.TravelPricingDetails.FirstOrDefaultAsync(t => t.OfferItemId == item.Id, ct);
        if (existing is null)
        {
            existing = TravelPricingDetailEntity.Create(
                item.Id, req.Method, req.OriginCityCode, req.DestinationCityCode, req.DistanceKm, req.PerKmRate, req.UnitCode);
            _db.TravelPricingDetails.Add(existing);
        }
        else
        {
            existing.SetValue(req.Method, req.OriginCityCode, req.DestinationCityCode, req.DistanceKm, req.PerKmRate, req.UnitCode);
        }

        await _db.SaveChangesAsync(ct);
        return ToDto(existing);
    }

    internal static TravelPricingDetailDto ToDto(TravelPricingDetailEntity e) => new()
    {
        OfferItemId = e.OfferItemId, Method = e.Method,
        OriginCityCode = e.OriginCityCode, DestinationCityCode = e.DestinationCityCode,
        DistanceKm = e.DistanceKm, PerKmRate = e.PerKmRate, UnitCode = e.UnitCode,
    };
}

/// <summary>BE-S4a — read the travel-pricing detail on one offer line.</summary>
public sealed class GetOfferLineTravelPricingQueryHandler
    : AizenQueryHandler<GetOfferLineTravelPricingQuery, TravelPricingDetailDto>
{
    private readonly ServiceRequestDbContext _db;
    public GetOfferLineTravelPricingQueryHandler(ServiceRequestDbContext db) => _db = db;

    public override async Task<TravelPricingDetailDto?> Handle(GetOfferLineTravelPricingQuery query, CancellationToken ct)
    {
        var e = await _db.TravelPricingDetails.AsNoTracking()
            .FirstOrDefaultAsync(t => t.OfferItemId == query.OfferItemId, ct);
        return e is null ? null : SetOfferLineTravelPricingCommandHandler.ToDto(e);
    }
}
