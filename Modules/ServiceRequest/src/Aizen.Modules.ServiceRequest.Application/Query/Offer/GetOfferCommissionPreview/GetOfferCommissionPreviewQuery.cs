using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using PayEnum = Aizen.Modules.Payment.Abstraction.Enum;
using SrEnum = Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.Query.Offer.GetOfferCommissionPreview;

/// <summary>
/// BE-S7 offer-builder preview: maps an offer's priced lines to the Payment line-commission contract and returns the
/// per-line resolved commission + transaction totals for display ("line 5000 → 12% → 600, your net 4400").
/// Compute-on-demand — nothing is persisted (rates change; the authoritative per-line record is S8/P8).
/// </summary>
public sealed class GetOfferCommissionPreviewQuery : AizenQuery<ResolveLineCommissionsRemoteCallResponse>
{
    public long  OfferId        { get; }
    /// <summary>Provider's plan (optional) so plan-tier rates resolve; null → category/global.</summary>
    public long? ProviderPlanId { get; }

    public GetOfferCommissionPreviewQuery(long offerId, long? providerPlanId = null)
    {
        OfferId = offerId;
        ProviderPlanId = providerPlanId;
    }
}

public sealed class GetOfferCommissionPreviewQueryHandler
    : AizenQueryHandler<GetOfferCommissionPreviewQuery, ResolveLineCommissionsRemoteCallResponse>
{
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceRequestRepository _srRepository;
    private readonly IPaymentModuleRemoteCall _paymentRemoteCall;
    private readonly IAizenInfoAccessor _info;

    public GetOfferCommissionPreviewQueryHandler(
        IServiceRequestOfferRepository offerRepository,
        IServiceRequestRepository srRepository,
        IPaymentModuleRemoteCall paymentRemoteCall,
        IAizenInfoAccessor info)
    {
        _offerRepository = offerRepository;
        _srRepository = srRepository;
        _paymentRemoteCall = paymentRemoteCall;
        _info = info;
    }

    public override async Task<ResolveLineCommissionsRemoteCallResponse?> Handle(
        GetOfferCommissionPreviewQuery query, CancellationToken ct)
    {
        var offer = await _offerRepository.GetByIdAsync(query.OfferId, ct)
            ?? throw new AizenBusinessException("Offer not found.");

        var sr = await _srRepository.GetByIdAsync(offer.ServiceRequestId, ct);
        var categoryCode = sr?.ServiceCategoryCode;

        // Map priced lines (S1 economics already computed on the persisted items) → Payment line-commission inputs.
        var lines = offer.Items
            .Where(i => i.ItemType != SrEnum.ServiceRequestOfferItemType.Discount)
            .Select(i => new ResolveLineCommissionInputDto
            {
                LineRef               = i.Id.ToString(),
                LineType              = MapLineType(i.ItemType),
                ProductCode           = null,
                CommissionEligibility = MapEligibility(i.CommissionEligibility),
                CategoryCode          = categoryCode,
                CommissionBaseAmount  = i.CommissionBaseAmount,
                LineProviderRevenue   = Math.Max(i.LineSubtotal - i.DiscountAmount, 0m),
            })
            .ToList();

        var request = new ResolveLineCommissionsRemoteCallRequest
        {
            ProviderProfileId = offer.ProviderProfileId,
            ProviderPlanId    = query.ProviderPlanId,
            CurrencyCode      = offer.CurrencyCode,
            Lines             = lines,
        };

        var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken;
        return await _paymentRemoteCall.ResolveLineCommissionsAsync(request, $"Bearer {rawToken}", ct);
    }

    // ── Mapping (SR → Payment.Abstraction dimensions) ────────────────────────────

    /// <summary>Maps the SR offer item type to the Payment commission LineType dimension. Other → null (no dim).</summary>
    public static PayEnum.LineType? MapLineType(SrEnum.ServiceRequestOfferItemType itemType) => itemType switch
    {
        SrEnum.ServiceRequestOfferItemType.Service or SrEnum.ServiceRequestOfferItemType.Labor
            or SrEnum.ServiceRequestOfferItemType.Installation or SrEnum.ServiceRequestOfferItemType.Inspection
            or SrEnum.ServiceRequestOfferItemType.EmergencyFee                          => PayEnum.LineType.Labor,
        SrEnum.ServiceRequestOfferItemType.Product or SrEnum.ServiceRequestOfferItemType.Consumable
                                                                                        => PayEnum.LineType.Part,
        SrEnum.ServiceRequestOfferItemType.Travel or SrEnum.ServiceRequestOfferItemType.Delivery
                                                                                        => PayEnum.LineType.Travel,
        SrEnum.ServiceRequestOfferItemType.ExternalService or SrEnum.ServiceRequestOfferItemType.EquipmentRental
            or SrEnum.ServiceRequestOfferItemType.MarinaOrLiftFee
            or SrEnum.ServiceRequestOfferItemType.OtherApprovedExpense                  => PayEnum.LineType.PassThrough,
        _                                                                               => null,
    };

    /// <summary>Maps SR line eligibility to the Payment.Abstraction line eligibility (1:1).</summary>
    public static PayEnum.LineCommissionEligibility MapEligibility(SrEnum.LineCommissionEligibility e) => e switch
    {
        SrEnum.LineCommissionEligibility.Eligible => PayEnum.LineCommissionEligibility.Eligible,
        SrEnum.LineCommissionEligibility.Exempt   => PayEnum.LineCommissionEligibility.Exempt,
        _                                         => PayEnum.LineCommissionEligibility.InheritFromCategory,
    };
}
