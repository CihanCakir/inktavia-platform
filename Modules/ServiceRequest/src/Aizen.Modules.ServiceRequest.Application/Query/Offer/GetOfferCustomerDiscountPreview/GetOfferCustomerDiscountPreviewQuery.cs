using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.Offer.GetOfferCustomerDiscountPreview;

/// <summary>Per-line customer-discount preview row (for the offer-builder display).</summary>
public sealed record OfferCustomerDiscountPreviewLine(
    string  LineRef,
    int     ItemType,
    bool    DiscountEligible,
    decimal CustomerDiscountAmount,
    decimal PlatformFundedAmount,
    decimal ProviderFundedAmount);

/// <summary>BE-S6 offer-builder preview — the resolved P6 customer discount allocated per line + funding, for display only.</summary>
public sealed record OfferCustomerDiscountPreviewResponse(
    bool    Found,
    string? RuleCode,
    decimal TotalCustomerDiscount,
    decimal TotalPlatformFundedDiscount,
    decimal TotalProviderFundedDiscount,
    IReadOnlyList<OfferCustomerDiscountPreviewLine> Lines);

/// <summary>
/// BE-S6 — resolves the P6 CustomerDiscountRule via the internal remote-call and runs the deterministic allocation + pre-tax
/// application on the offer (transiently — the query never SaveChanges, so nothing is persisted; no budget reserve, no
/// snapshot — all P8). Mirrors the S7 commission preview. Provider consent is assumed granted for the display (optimistic).
/// </summary>
public sealed class GetOfferCustomerDiscountPreviewQuery : AizenQuery<OfferCustomerDiscountPreviewResponse>
{
    public long OfferId { get; }
    public GetOfferCustomerDiscountPreviewQuery(long offerId) => OfferId = offerId;
}

public sealed class GetOfferCustomerDiscountPreviewQueryHandler
    : AizenQueryHandler<GetOfferCustomerDiscountPreviewQuery, OfferCustomerDiscountPreviewResponse>
{
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceRequestRepository      _srRepository;
    private readonly IPaymentModuleRemoteCall       _paymentRemoteCall;
    private readonly OfferCalculationService        _calc;
    private readonly IAizenInfoAccessor             _info;

    public GetOfferCustomerDiscountPreviewQueryHandler(
        IServiceRequestOfferRepository offerRepository,
        IServiceRequestRepository srRepository,
        IPaymentModuleRemoteCall paymentRemoteCall,
        OfferCalculationService calc,
        IAizenInfoAccessor info)
    {
        _offerRepository   = offerRepository;
        _srRepository      = srRepository;
        _paymentRemoteCall = paymentRemoteCall;
        _calc              = calc;
        _info              = info;
    }

    public override async Task<OfferCustomerDiscountPreviewResponse?> Handle(
        GetOfferCustomerDiscountPreviewQuery query, CancellationToken ct)
    {
        var offer = await _offerRepository.GetByIdAsync(query.OfferId, ct)
            ?? throw new AizenBusinessException("Offer not found.");
        var sr = await _srRepository.GetByIdAsync(offer.ServiceRequestId, ct);

        // Baseline calc (no customer discount) → per-line post-provider-discount pre-tax base.
        _calc.Calculate(offer);
        var pricedLines = offer.Items.Where(i => i.ItemType != ServiceRequestOfferItemType.Discount).ToList();
        var eligibleBase = pricedLines
            .Where(l => l.LineDiscountEligibility != LineDiscountEligibility.Exempt)
            .Sum(l => Math.Max(l.LineSubtotal - l.DiscountAmount, 0m));

        // Resolve the P6 rule + requested amount via the internal remote-call.
        var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken;
        var resolved = await _paymentRemoteCall.ResolveCustomerDiscountAsync(new ResolveCustomerDiscountRemoteCallRequest
        {
            CustomerPlanId            = null,                       // narrow core: no participant plan on the SR side
            CategoryCode              = sr?.ServiceCategoryCode,
            CurrencyCode              = offer.CurrencyCode,
            EligibleServiceBaseAmount = eligibleBase,
        }, $"Bearer {rawToken}", ct);

        if (resolved is null || !resolved.Found || resolved.RequestedDiscountAmount <= 0m)
            return Empty(pricedLines);

        // Apply the resolved discount (consent optimistic for display) and read back the per-line allocation.
        _calc.Calculate(offer, new CustomerDiscountSpec(
            RequestedAmount: resolved.RequestedDiscountAmount,
            FundingMode:     resolved.FundingMode,
            PlatformRate:    resolved.PlatformFundingRate,
            ProviderRate:    resolved.ProviderFundingRate,
            ProviderConsent: true,
            RuleCode:        resolved.RuleCode));

        var lines = pricedLines.Select(l => new OfferCustomerDiscountPreviewLine(
            LineRef:                l.Id.ToString(),
            ItemType:               (int)l.ItemType,
            DiscountEligible:       l.LineDiscountEligibility != LineDiscountEligibility.Exempt,
            CustomerDiscountAmount: l.CustomerDiscountAmount,
            PlatformFundedAmount:   l.PlatformFundedDiscountAmount,
            ProviderFundedAmount:   l.ProviderFundedDiscountAmount)).ToList();

        return new OfferCustomerDiscountPreviewResponse(
            Found: true, RuleCode: resolved.RuleCode,
            TotalCustomerDiscount:       offer.TotalCustomerDiscount,
            TotalPlatformFundedDiscount: offer.TotalPlatformFundedDiscount,
            TotalProviderFundedDiscount: offer.TotalProviderFundedDiscount,
            Lines: lines);
    }

    private static OfferCustomerDiscountPreviewResponse Empty(
        IEnumerable<Domain.Entities.Offer.ServiceRequestOfferItemEntity> pricedLines)
        => new(Found: false, RuleCode: null, 0m, 0m, 0m,
            pricedLines.Select(l => new OfferCustomerDiscountPreviewLine(
                l.Id.ToString(), (int)l.ItemType, l.LineDiscountEligibility != LineDiscountEligibility.Exempt, 0m, 0m, 0m)).ToList());
}
