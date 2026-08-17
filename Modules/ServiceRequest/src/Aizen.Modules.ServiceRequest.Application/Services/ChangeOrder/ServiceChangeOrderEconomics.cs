using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Application.Query.Offer.GetOfferCommissionPreview;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using SrEnum = Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.Services.ChangeOrder;

/// <summary>
/// BE-S11b — turns a change order's proposed lines into the P8 economics request for the <b>incremental</b> amount. It
/// reuses the shared <see cref="OfferCalculationService"/> (the same line-subtotal/tax/commission-base math the offer runs)
/// over a <b>transient</b> offer built from the change-order lines, then maps to the same
/// <see cref="CalculateServiceRequestEconomicsRemoteCallRequest"/> the acceptance path uses — with a distinct idempotency
/// key <c>SR-{sr}-OFFER-{offer}-CO-{id}</c> so Payment produces a NEW snapshot + a NEW incremental escrow (never touching
/// the accepted snapshot, never returning the original escrow). No new economics math lives here.
/// </summary>
public static class ServiceChangeOrderEconomics
{
    /// <summary>The Payment idempotency / escrow context ref for a change order — matches the reduction path's ref.</summary>
    public static string ContextRef(long serviceRequestId, long acceptedOfferId, long changeOrderId)
        => $"SR-{serviceRequestId}-OFFER-{acceptedOfferId}-CO-{changeOrderId}";

    /// <summary>
    /// Builds a transient (unpersisted) offer carrying the change order's proposed lines, in the accepted offer's identity +
    /// the change order's currency. Used only to run the shared calculation; never saved.
    /// </summary>
    public static ServiceRequestOfferEntity BuildTransientOffer(
        ServiceRequestOfferEntity acceptedOffer, ServiceChangeOrderEntity changeOrder)
    {
        var transient = ServiceRequestOfferEntity.Create(
            serviceRequestId:         acceptedOffer.ServiceRequestId,
            providerProfileId:        acceptedOffer.ProviderProfileId,
            providerUserId:           acceptedOffer.ProviderUserId,
            totalAmount:              0m,
            currencyCode:             changeOrder.CurrencyCode,
            description:              null,
            providerNotes:            null,
            estimatedStartDate:       null,
            estimatedEndDate:         null,
            estimatedDurationMinutes: null,
            expiresAt:                null,
            offerType:                acceptedOffer.OfferType);

        var items = changeOrder.Items
            .OrderBy(i => i.SortOrder)
            .Select((i, idx) => ServiceRequestOfferItemEntity.Create(
                serviceRequestOfferId: 0,
                itemType:              i.ItemType,
                title:                 i.Title,
                description:           i.Description,
                quantity:              i.Quantity,
                unitPrice:             i.UnitPrice,
                currencyCode:          i.CurrencyCode,
                sortOrder:             idx,                       // stable, distinct LineRef for a transient (Id-less) item
                unitCode:              i.UnitCode,
                taxRate:               i.TaxRate,
                discountType:          i.DiscountType,
                discountValue:         i.DiscountValue,
                pricingMethod:         i.PricingMethod,
                commissionEligibility: i.CommissionEligibility,
                discountEligibility:   i.LineDiscountEligibility))
            .ToList();

        transient.ReplaceItems(items);
        return transient;
    }

    /// <summary>
    /// Runs the shared calculation over the change order's lines and projects them to the P8 request with the change order's
    /// context ref. LineRef is the (distinct) SortOrder — a transient offer has Id-less items. Discount lines are excluded
    /// (mirrors acceptance). No FX/attributes/travel: change-order lines are priced in the settlement currency.
    /// </summary>
    public static CalculateServiceRequestEconomicsRemoteCallRequest BuildIncrementalEconomicsRequest(
        ServiceRequestEntity sr, ServiceRequestOfferEntity acceptedOffer, ServiceChangeOrderEntity changeOrder,
        OfferCalculationService calc)
    {
        var transient = BuildTransientOffer(acceptedOffer, changeOrder);
        calc.Calculate(transient);

        var lines = transient.Items
            .Where(i => i.ItemType != SrEnum.ServiceRequestOfferItemType.Discount)
            .Select(i =>
            {
                var providerRevenue = Math.Max(i.LineSubtotal - i.DiscountAmount, 0m);
                return new CalculateServiceRequestEconomicsLineDto
                {
                    LineRef                 = i.SortOrder.ToString(),
                    ItemType                = (int)i.ItemType,
                    PricingMethod           = (int)i.PricingMethod,
                    CommissionLineType      = GetOfferCommissionPreviewQueryHandler.MapLineType(i.ItemType),
                    CommissionEligibility   = GetOfferCommissionPreviewQueryHandler.MapEligibility(i.CommissionEligibility),
                    ProductCode             = null,
                    LineGrossBeforeDiscount = providerRevenue,
                    LineVat                 = i.TaxAmount,
                    CommissionBaseAmount    = i.CommissionBaseAmount,
                    LineProviderRevenue     = providerRevenue,
                    DiscountEligible        = i.LineDiscountEligibility != SrEnum.LineDiscountEligibility.Exempt,
                    Attributes              = new(),
                    Fx                      = null,
                    Travel                  = null,
                };
            })
            .ToList();

        return new CalculateServiceRequestEconomicsRemoteCallRequest
        {
            IdempotencyKey    = ContextRef(sr.Id, acceptedOffer.Id, changeOrder.Id),
            ServiceRequestId  = sr.Id,
            OfferId           = acceptedOffer.Id,
            ProviderProfileId = acceptedOffer.ProviderProfileId,
            CustomerProfileId = sr.OwnerUserId,
            CustomerPlanId    = null,
            CategoryCode      = sr.ServiceCategoryCode,
            CurrencyCode      = changeOrder.CurrencyCode,
            Lines             = lines,
        };
    }

    /// <summary>The change order's customer-facing amount (grand total of its lines) — the reduction amount for a Decrease,
    /// or a cross-check for an Increase. Runs the shared calculation only; no persistence.</summary>
    public static decimal ComputeLineGrandTotal(
        ServiceRequestOfferEntity acceptedOffer, ServiceChangeOrderEntity changeOrder, OfferCalculationService calc)
    {
        var transient = BuildTransientOffer(acceptedOffer, changeOrder);
        calc.Calculate(transient);
        return transient.GrandTotal;
    }
}
