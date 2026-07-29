using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Model.Result;

namespace Aizen.Modules.Payment.Application.Commands.PurchaseOfferBoost;

/// <summary>
/// BE-P11 §9.2 — a provider purchases an OFFER_BOOST_7D for one of their offers. Resolves the active product + point-in-time
/// price, snapshots them onto a Pending <see cref="PremiumPurchaseEntity"/>, and initiates a <b>non-marketplace</b> checkout
/// (SubMerchantKey null → whole amount to the main merchant). <b>No entitlement is created until the success webhook.</b>
/// </summary>
public sealed class PurchaseOfferBoostCommand : AizenCommand<PurchaseOfferBoostResult>
{
    public required long   ProviderProfileId { get; init; }
    public required long   OfferId           { get; init; }
    public          string CurrencyCode      { get; init; } = "TRY";
}
