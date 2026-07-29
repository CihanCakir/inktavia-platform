using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Services;
using Aizen.Modules.Payment.Domain.Entities.Premium;
using Aizen.Modules.Payment.Domain.Entities.Transaction;
using Aizen.Modules.Payment.Domain.Interface.Repository;
using Aizen.Modules.Payment.Domain.Money;
using Aizen.Modules.Payment.Repository.Seed;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Application.Commands.PurchaseOfferBoost;

[DocumentationInfo("Purchase offer boost command handler (BE-P11)",
    "Resolves OFFER_BOOST_7D + its point-in-time price, snapshots them onto a Pending PremiumPurchase, and initiates a " +
    "non-marketplace checkout (SubMerchantKey null). No entitlement is created until the success webhook (§9.2).")]
public sealed class PurchaseOfferBoostCommandHandler
    : AizenCommandHandler<PurchaseOfferBoostCommand, PurchaseOfferBoostResult>
{
    private readonly IPremiumProductRepository      _products;
    private readonly IPremiumProductPriceRepository _prices;
    private readonly IPremiumPurchaseRepository     _purchases;
    private readonly IPaymentTransactionRepository  _transactions;
    private readonly PaymentGatewayResolver         _gatewayResolver;
    private readonly ILogger<PurchaseOfferBoostCommandHandler> _logger;

    public PurchaseOfferBoostCommandHandler(
        IPremiumProductRepository      products,
        IPremiumProductPriceRepository prices,
        IPremiumPurchaseRepository     purchases,
        IPaymentTransactionRepository  transactions,
        PaymentGatewayResolver         gatewayResolver,
        ILogger<PurchaseOfferBoostCommandHandler> logger)
    {
        _products        = products;
        _prices          = prices;
        _purchases       = purchases;
        _transactions    = transactions;
        _gatewayResolver = gatewayResolver;
        _logger          = logger;
    }

    public override async Task<PurchaseOfferBoostResult?> Handle(
        PurchaseOfferBoostCommand request, CancellationToken ct)
    {
        var now      = DateTime.UtcNow;
        var currency = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "TRY" : request.CurrencyCode.ToUpperInvariant();

        // ── Resolve the active product (OFFER_BOOST_7D) ──────────────────────────
        var product = await _products.GetByCodeAsync(PremiumProductSeed.OfferBoostCode, ct);
        if (product is null || !product.IsPurchasable)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductNotFound,
                $"Premium product {PremiumProductSeed.OfferBoostCode} not found or inactive.");

        // ── Resolve the point-in-time price (§13.9 — snapshotted onto the purchase) ──
        var price = await _prices.ResolveAsync(product.Id, currency, now, ct)
            ?? throw new AizenBusinessException((int)PaymentErrorCode.PremiumProductPriceNotFound,
                $"No active premium price for {product.Code} in {currency} at {now:o}.");

        // ── Duplicate guard (§9.2): one Pending/Paid boost per (provider, offer) ──
        var duplicate = await _purchases.GetActiveOrPendingAsync(request.ProviderProfileId, request.OfferId, product.Id, ct);
        if (duplicate is not null)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumDuplicateActiveBoost,
                $"A {duplicate.Status} boost already exists for provider {request.ProviderProfileId} offer {request.OfferId}.");

        var unitPrice = MoneyMath.Round(price.PriceAmount);

        // ── Create the Pending purchase (snapshot price + duration + offer) ──────
        var purchaseCode = $"BST-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var purchase = PremiumPurchaseEntity.Create(
            providerProfileId:             request.ProviderProfileId,
            premiumProductId:              product.Id,
            productCodeSnapshot:           product.Code,
            premiumProductPriceIdSnapshot: price.Id,
            unitPriceSnapshot:             unitPrice,
            currencyCodeSnapshot:          currency,
            durationDaysSnapshot:          product.DurationDays,
            contextRef:                    request.OfferId,
            purchaseCode:                  purchaseCode);
        await _purchases.AddAsync(purchase, ct);
        await _purchases.SaveChangesAsync(ct);   // materialise purchase.Id

        // ── Create the PendingIntent payment transaction (non-marketplace) ───────
        var idempotencyKey  = $"BOOST-{request.ProviderProfileId}-OFFER-{request.OfferId}";
        var transactionCode = $"TXN-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..24];
        var tx = PaymentTransactionEntity.Create(
            transactionCode:        transactionCode,
            transactionType:        TransactionType.PremiumBoostPurchase,
            contextType:            TransactionContextType.Premium,
            contextId:              request.OfferId,
            contextSubId:           null,
            payerProfileId:         request.ProviderProfileId,   // the provider pays for their own boost
            recipientProfileId:     null,                        // non-marketplace → whole amount to the main merchant (Inktavia)
            grossAmount:            unitPrice,
            commissionAmount:       0m,                           // §19.4 — premium never touches commission
            commissionRateSnapshot: 0m,
            vatOnCommission:        0m,
            netPayoutAmount:        0m,                           // no provider net → no split
            discountAmount:         0m,
            currencyCode:           currency,
            gatewayProvider:        _gatewayResolver.Resolve().ProviderKey,
            idempotencyKey:         idempotencyKey,
            escrowRequired:         false);
        await _transactions.AddAsync(tx, ct);
        await _transactions.SaveChangesAsync(ct);   // materialise tx.Id for the basket id / purchase link

        // ── Non-marketplace checkout (SubMerchantKey null → split-guard skips) ───
        var gateway = _gatewayResolver.Resolve();
        var init = await gateway.InitiateCheckoutAsync(new CheckoutInitInput
        {
            TransactionId     = tx.Id,
            IdempotencyKey    = idempotencyKey,
            GrossAmount       = unitPrice,
            CurrencyCode      = currency,
            PayerProfileId    = request.ProviderProfileId,
            RecipientProfileId = null,
            SubMerchantKey    = null,          // ← non-marketplace: no split, whole amount to main merchant
            ProviderNetAmount = 0m,            // no provider share
            ExpectedRetainedAmount = null,     // premium revenue → retained == gross, not asserted against a snapshot
            Context           = TransactionContext.ForPremiumOffer(request.OfferId),
            EscrowRequired    = false,
            Description       = $"Offer boost (7 days) — offer {request.OfferId}",
        }, ct);

        if (!init.IsSuccess)
            throw new AizenBusinessException((int)PaymentErrorCode.GatewayInitiationFailed);

        // Attach the gateway reference so the success webhook can locate this PendingIntent tx and capture + activate.
        tx.AttachGatewayReference(init.GatewayReference);
        _transactions.Update(tx);
        purchase.LinkTransaction(tx.Id);
        _purchases.Update(purchase);
        // SaveChanges handled by AizenCommandHandlerDecorator.

        _logger.LogInformation(
            "Offer boost purchase initiated. Purchase={Code} Tx={TxId} Offer={Offer} Provider={Provider} Price={Price} {Cur} (Pending — no entitlement yet).",
            purchase.PurchaseCode, tx.Id, request.OfferId, request.ProviderProfileId, unitPrice, currency);

        return new PurchaseOfferBoostResult(
            purchase.Id, purchase.PurchaseCode, tx.Id, init.GatewayReference,
            unitPrice, currency, product.DurationDays, init.CheckoutFormContent, init.RedirectUrl);
    }
}
