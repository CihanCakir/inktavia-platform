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
    private readonly Services.Fx.OfferFxResolver _fxResolver;

    public PreviewOfferCommandHandler(
        IAizenInfoAccessor info, OfferCalculationService calculation, Services.Fx.OfferFxResolver fxResolver)
    {
        _info = info;
        _calculation = calculation;
        _fxResolver = fxResolver;
    }

    public override async Task<PreviewOfferResponse?> Handle(PreviewOfferCommand command, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var req = command.Request;

        // Build a transient offer (not persisted)
        var offer = ServiceRequestOfferEntity.Create(
            command.ServiceRequestId, providerProfileId, _info.UserInfoAccessor.UserInfo.UserId,
            0, req.CurrencyCode, req.Description, req.ProviderNotes,
            req.EstimatedStartDate, req.EstimatedEndDate, req.EstimatedDurationMinutes, req.ExpiresAt);

        var items = req.Items.Select((item, idx) => ServiceRequestOfferItemEntity.Create(
            0, item.ItemType, item.Title, item.Description,
            item.Quantity, item.UnitPrice, item.CurrencyCode, item.SortOrder > 0 ? item.SortOrder : idx,
            item.UnitCode, item.TaxRate, item.DiscountType, item.DiscountValue,
            item.PricingMethod, item.CommissionEligibility
        ));

        offer.ReplaceItems(items);

        // BE-S3d — mixed source currencies are allowed now: each foreign line converts to TRY at the preview instant so the
        // provider sees both the source figure ("500 EUR") and the TRY the customer will be charged. Fail-loud on a missing
        // rate, same as submit; a TRY-only preview resolves nothing (byte-identical to pre-S3).
        await _fxResolver.ResolveAndConvertAsync(offer, DateTime.UtcNow, ct);

        _calculation.Calculate(offer);

        return new PreviewOfferResponse(offer.ToDto());
    }
}
