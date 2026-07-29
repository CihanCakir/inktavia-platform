using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Premium;

/// <summary>
/// BE-P11 §9.2 — the entitlement granted by a paid premium purchase. <b>At most one per purchase</b> (unique
/// <see cref="PremiumPurchaseId"/>). Created + Activated by the success webhook only (never Active before it), revoked on
/// refund, expired past <see cref="ExpiresAt"/>. Offer-scoped via <see cref="ContextRef"/>. Guarded transitions.
/// </summary>
[DocumentationInfo("Premium entitlement entity",
    "The offer-scoped entitlement from a paid premium purchase. ≤1 per purchase; Active only after the success webhook; refund→Revoked; past-expiry→Expired.")]
public sealed class PremiumEntitlementEntity : AizenEntityWithAudit
{
    public long                     PremiumPurchaseId  { get; private set; }   // unique — ≤1 entitlement per purchase
    public long                     ProviderProfileId  { get; private set; }
    public string                   ProductCodeSnapshot { get; private set; } = default!;
    public long                     ContextRef         { get; private set; }   // offer id
    public PremiumEntitlementStatus Status             { get; private set; }
    public DateTime?                StartsAt           { get; private set; }
    public DateTime?                ExpiresAt          { get; private set; }
    public DateTime?                RevokedAt          { get; private set; }
    public string?                  RevocationReason   { get; private set; }

    private PremiumEntitlementEntity() { }

    /// <summary>Creates an <b>Inactive</b> entitlement (never Active on creation — §9.2). Activate via the success webhook.</summary>
    public static PremiumEntitlementEntity CreateInactive(
        long premiumPurchaseId, long providerProfileId, string productCodeSnapshot, long contextRef)
        => new()
        {
            PremiumPurchaseId   = premiumPurchaseId,
            ProviderProfileId   = providerProfileId,
            ProductCodeSnapshot = productCodeSnapshot,
            ContextRef          = contextRef,
            Status              = PremiumEntitlementStatus.Inactive,
            IsActive            = true,
        };

    /// <summary>§9.2 — Inactive → Active (success webhook). Idempotent if already Active for the same window.</summary>
    public void Activate(DateTime startsAt, DateTime expiresAt)
    {
        if (Status == PremiumEntitlementStatus.Active) return;
        if (Status != PremiumEntitlementStatus.Inactive)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumEntitlementInvalidTransition,
                $"Cannot Activate from {Status}.");
        if (expiresAt <= startsAt)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumEntitlementInvalidTransition,
                "ExpiresAt must be after StartsAt.");
        Status    = PremiumEntitlementStatus.Active;
        StartsAt  = startsAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>§9.2 — refund/admin revoke. Idempotent (already Revoked → no-op). Expired/Inactive cannot be revoked.</summary>
    public void Revoke(string reason)
    {
        if (Status == PremiumEntitlementStatus.Revoked) return;
        if (Status != PremiumEntitlementStatus.Active)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumEntitlementInvalidTransition,
                $"Cannot Revoke from {Status}.");
        Status           = PremiumEntitlementStatus.Revoked;
        RevokedAt        = DateTime.UtcNow;
        RevocationReason = reason;
    }

    /// <summary>§13.9 — Active → Expired (past ExpiresAt). Idempotent (already Expired → no-op).</summary>
    public void Expire()
    {
        if (Status == PremiumEntitlementStatus.Expired) return;
        if (Status != PremiumEntitlementStatus.Active)
            throw new AizenBusinessException((int)PaymentErrorCode.PremiumEntitlementInvalidTransition,
                $"Cannot Expire from {Status}.");
        Status = PremiumEntitlementStatus.Expired;
    }

    /// <summary>Live now = Active and within [StartsAt, ExpiresAt).</summary>
    public bool IsCurrentlyActive(DateTime atUtc)
        => Status == PremiumEntitlementStatus.Active
        && StartsAt.HasValue && ExpiresAt.HasValue
        && StartsAt.Value <= atUtc && ExpiresAt.Value > atUtc;
}
