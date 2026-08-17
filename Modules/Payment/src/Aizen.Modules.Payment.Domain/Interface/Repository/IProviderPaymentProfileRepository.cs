using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;

namespace Aizen.Modules.Payment.Domain.Interface.Repository;

/// <summary>
/// Split-eligibility read model (BE-I1) — the single gate signal the P8 escrow path and the SR eligibility remote-call
/// consume. <see cref="HasProfile"/> false means the provider never onboarded a payment profile (→ not eligible).
/// </summary>
public sealed record ProviderSplitEligibility(
    bool                               HasProfile,
    bool                               IsSplitEligible,
    ProviderSubMerchantOnboardingStatus OnboardingStatus,
    string?                            SubMerchantKey = null);

public interface IProviderPaymentProfileRepository
{
    Task<ProviderPaymentProfileEntity?> GetByProviderProfileIdAsync(long providerProfileId, CancellationToken ct = default);
    Task<bool> ExistsAsync(long providerProfileId, CancellationToken ct = default);

    /// <summary>BE-I1 — reads just the split-eligibility signal for a provider (no entity tracking needed).</summary>
    Task<ProviderSplitEligibility> GetSplitEligibilityAsync(long providerProfileId, CancellationToken ct = default);

    /// <summary>BE-I1 — the admin sub-merchant onboarding review queue: profiles filtered by onboarding status, paged
    /// (default: everything except NotStarted/Verified when <paramref name="status"/> is null — i.e. items needing attention).</summary>
    Task<(List<ProviderPaymentProfileEntity> Items, int Total)> GetOnboardingQueueAsync(
        ProviderSubMerchantOnboardingStatus? status, int skip, int take, CancellationToken ct = default);

    Task AddAsync(ProviderPaymentProfileEntity entity, CancellationToken ct = default);
    void Update(ProviderPaymentProfileEntity entity);
    Task SaveChangesAsync(CancellationToken ct = default);
}
