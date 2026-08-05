using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using SrEnum = Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.Query.Offer.GetOfferPartTermsPreview;

/// <summary>
/// BE-S5c — offer-builder part-terms preview. Compute-on-demand (nothing persisted, no SR migration — same discipline as
/// S7's <c>GetOfferCommissionPreview</c>): maps the offer's Product/Consumable lines to the Payment resolve request, forwards
/// the caller's bearer token, and returns the <b>cost-free</b> <see cref="ResolvePartLineAllowancesRemoteCallResponse"/>.
/// SR never sees supplier cost / dealer margin — only the allowance (max discount + funded split + min-receivable).
/// </summary>
public sealed class GetOfferPartTermsPreviewQuery : AizenQuery<ResolvePartLineAllowancesRemoteCallResponse>
{
    public long OfferId { get; }
    public GetOfferPartTermsPreviewQuery(long offerId) => OfferId = offerId;
}

[DocumentationInfo("GetOfferPartTermsPreviewQueryHandler",
    "Compute-on-demand: maps the offer's Product/Consumable lines → Payment part-terms resolve request, forwards the bearer " +
    "token, returns the cost-free allowance per part line. No persistence. Cost never crosses to SR (§20.9).")]
public sealed class GetOfferPartTermsPreviewQueryHandler
    : AizenQueryHandler<GetOfferPartTermsPreviewQuery, ResolvePartLineAllowancesRemoteCallResponse>
{
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceRequestRepository _srRepository;
    private readonly IPaymentModuleRemoteCall _paymentRemoteCall;
    private readonly IAizenInfoAccessor _info;

    public GetOfferPartTermsPreviewQueryHandler(
        IServiceRequestOfferRepository offerRepository, IServiceRequestRepository srRepository,
        IPaymentModuleRemoteCall paymentRemoteCall, IAizenInfoAccessor info)
    {
        _offerRepository = offerRepository; _srRepository = srRepository;
        _paymentRemoteCall = paymentRemoteCall; _info = info;
    }

    public override async Task<ResolvePartLineAllowancesRemoteCallResponse?> Handle(
        GetOfferPartTermsPreviewQuery query, CancellationToken ct)
    {
        var offer = await _offerRepository.GetByIdAsync(query.OfferId, ct)
            ?? throw new AizenBusinessException("Offer not found.");

        var sr = await _srRepository.GetByIdAsync(offer.ServiceRequestId, ct);
        var categoryCode = sr?.ServiceCategoryCode;

        // Only Product / Consumable lines are "part" lines. ProductCode/Brand are not modelled on the offer line yet, so the
        // preview resolves at provider/category/global specificity; when catalog linkage lands they can be threaded in.
        var lines = offer.Items
            .Where(IsPartLine)
            .Select(i => new ResolvePartLineInputDto
            {
                LineRef      = i.Id.ToString(),
                ProductCode  = null,
                Brand        = null,
                CategoryCode = categoryCode,
            })
            .ToList();

        var request = new ResolvePartLineAllowancesRemoteCallRequest
        {
            ProviderProfileId = offer.ProviderProfileId,
            CurrencyCode      = offer.CurrencyCode,
            AsOfUtc           = null,
            Lines             = lines,
        };

        var rawToken = _info.UserInfoAccessor.UserInfo.AccessToken;
        return await _paymentRemoteCall.ResolvePartLineAllowancesAsync(request, $"Bearer {rawToken}", ct);
    }

    /// <summary>A "part" line is a Product or Consumable line (the S5 scope).</summary>
    public static bool IsPartLine(Domain.Entities.Offer.ServiceRequestOfferItemEntity item)
        => IsPartLineType(item.ItemType);

    /// <summary>The S5 "part" scope: Product or Consumable line types.</summary>
    public static bool IsPartLineType(SrEnum.ServiceRequestOfferItemType itemType)
        => itemType is SrEnum.ServiceRequestOfferItemType.Product
                    or SrEnum.ServiceRequestOfferItemType.Consumable;
}
