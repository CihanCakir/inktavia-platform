using Aizen.Core.Domain;
using Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;

namespace Aizen.Modules.Payment.Domain.Entities.Premium;

/// <summary>
/// BE-P11 §9.1 — a sellable premium product (MVP: <c>OFFER_BOOST_7D</c>). A product grants a single, typed entitlement
/// (<see cref="EntitlementType"/>) for <see cref="DurationDays"/> days. Pricing is date-versioned in
/// <see cref="PremiumProductPriceEntity"/> (BE-P4 mirror), never on the product itself.
/// <para><b>§19.4 (binding):</b> a premium product provides visibility/ranking/showcase only — it MUST NOT be referenced
/// by the P2/P7 commission resolvers or the economics snapshot. It lives entirely in <c>Entities/Premium/</c>.</para>
/// </summary>
[DocumentationInfo("Premium product entity",
    "A sellable premium product (OFFER_BOOST_7D). Grants one typed entitlement for DurationDays. Fully decoupled from commission (§19.4).")]
public sealed class PremiumProductEntity : AizenEntityWithAudit
{
    public string                 Code            { get; private set; } = default!;   // unique, e.g. OFFER_BOOST_7D
    public string                 Name            { get; private set; } = default!;
    public PremiumEntitlementType EntitlementType { get; private set; }
    public int                    DurationDays    { get; private set; }               // 7 for OFFER_BOOST_7D
    public PremiumProductStatus   Status          { get; private set; }
    public string?                Description      { get; private set; }

    private PremiumProductEntity() { }

    public static PremiumProductEntity Create(
        string code, string name, PremiumEntitlementType entitlementType, int durationDays,
        PremiumProductStatus status = PremiumProductStatus.Active, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.", nameof(code));
        if (durationDays <= 0)               throw new ArgumentException("DurationDays must be positive.", nameof(durationDays));

        return new PremiumProductEntity
        {
            Code            = code.Trim().ToUpperInvariant(),
            Name            = name,
            EntitlementType = entitlementType,
            DurationDays    = durationDays,
            Status          = status,
            Description     = description,
            IsActive        = status == PremiumProductStatus.Active,
        };
    }

    public bool IsPurchasable => Status == PremiumProductStatus.Active && IsActive;

    public void Activate()   { Status = PremiumProductStatus.Active;   IsActive = true;  }
    public void Deactivate() { Status = PremiumProductStatus.Inactive; IsActive = false; }

    /// <summary>BE-P11 admin edit — the tunable display/duration fields (Code + EntitlementType are immutable).</summary>
    public void Update(string name, int durationDays, string? description)
    {
        if (durationDays <= 0) throw new ArgumentException("DurationDays must be positive.", nameof(durationDays));
        Name        = name;
        DurationDays = durationDays;
        Description = description;
    }
}
