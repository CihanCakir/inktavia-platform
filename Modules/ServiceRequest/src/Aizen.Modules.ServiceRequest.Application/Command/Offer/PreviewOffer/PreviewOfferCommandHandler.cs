using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer.PreviewOffer;

/// <summary>
/// Same inputs as SaveOfferDraft, runs the calculation service, returns the computed offer without persisting.
/// </summary>
public sealed class PreviewOfferCommandHandler : AizenCommandHandler<PreviewOfferCommand, PreviewOfferResponse>
{
    private readonly IAizenInfoAccessor _info;
    private readonly OfferCalculationService _calculation;

    public PreviewOfferCommandHandler(IAizenInfoAccessor info, OfferCalculationService calculation)
    {
        _info = info;
        _calculation = calculation;
    }

    public override Task<PreviewOfferResponse?> Handle(PreviewOfferCommand command, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var req = command.Request;

        // Validate mixed currency
        var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { req.CurrencyCode.ToUpperInvariant() };
        foreach (var item in req.Items)
            currencies.Add(item.CurrencyCode.ToUpperInvariant());
        if (currencies.Count > 1)
            throw new AizenBusinessException("Mixed currencies are not allowed.");

        // Build a transient offer (not persisted)
        var offer = ServiceRequestOfferEntity.Create(
            command.ServiceRequestId, providerProfileId, _info.UserInfoAccessor.UserInfo.UserId,
            0, req.CurrencyCode, req.Description, req.ProviderNotes,
            req.EstimatedStartDate, req.EstimatedEndDate, req.EstimatedDurationMinutes, req.ExpiresAt);

        var items = req.Items.Select((item, idx) => ServiceRequestOfferItemEntity.Create(
            0, item.ItemType, item.Title, item.Description,
            item.Quantity, item.UnitPrice, item.CurrencyCode, item.SortOrder > 0 ? item.SortOrder : idx,
            item.UnitCode, item.TaxRate, item.DiscountType, item.DiscountValue
        ));

        offer.ReplaceItems(items);
        _calculation.Calculate(offer);

        return Task.FromResult<PreviewOfferResponse?>(new PreviewOfferResponse(offer.ToDto()));
    }
}
