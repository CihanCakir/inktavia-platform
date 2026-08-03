using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// BE-I1 — seeds ProviderPaymentProfile sub-merchant onboarding demo rows so the admin KYC review queue
/// (<c>GET providers/sub-merchant/onboarding-queue</c>) has realistic data in the <b>runtime</b> DB (<c>inktavia_store</c>).
///
/// Mirrors <see cref="PayoutRecordMockSeed"/>: dev/local demo data, idempotent (skips when the canonical demo rows
/// already exist), no migration. Uses stable demo provider-profile ids (990001–990004) so it is the single source of
/// demo truth — it supersedes the ad-hoc #990001–990003 rows used during the ops-queues verification.
///
/// Coverage (masked LegalName/TaxNumber/SubMerchantKey; IsSplitEligible/HasIban derived by the entity):
///   990001  Demo Marine Yakıt A.Ş.       → SubMerchantCreated  (awaiting verify, split-eligible)
///   990002  Demo Liman Hizmetleri Ltd.   → Verified            (our review complete, split-eligible)
///   990003  Demo Tekne Bakım A.Ş.        → Rejected            (rejected during review, not split-eligible)
///   990004  Demo Balıkçılık Koop.        → SubMerchantCreated  (second awaiting-review row, split-eligible)
/// </summary>
public sealed class SubMerchantOnboardingMockSeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<SubMerchantOnboardingMockSeed> _logger;

    // Stable demo provider-profile ids — the single source of demo onboarding truth.
    private static readonly long[] DemoProviderIds = { 990001, 990002, 990003, 990004 };

    public SubMerchantOnboardingMockSeed(PaymentDbContext db, ILogger<SubMerchantOnboardingMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Reconcile: drop any prior ad-hoc rows in the demo id range so the seed owns the canonical states.
        var stale = await _db.PaymentProfiles.Where(p => DemoProviderIds.Contains(p.ProviderProfileId)).ToListAsync(ct);
        if (stale.Count > 0)
        {
            // Already canonical (all four present) → nothing to do.
            if (stale.Count == DemoProviderIds.Length)
            {
                _logger.LogDebug("Sub-merchant onboarding demo seed skipped — canonical rows already present.");
                return;
            }
            _db.PaymentProfiles.RemoveRange(stale);
            await _db.SaveChangesAsync(ct);
        }

        var rows = new List<ProviderPaymentProfileEntity>
        {
            BuildSubMerchantCreated(990001, "Demo Marine Yakıt A.Ş.",     "5551234321", "SMK-LIVE-AX7K9F21", "ACC-990001", "4242"),
            BuildVerified          (990002, "Demo Liman Hizmetleri Ltd.", "6669876123", "SMK-LIVE-QW3E8R55", "ACC-990002", "1881"),
            BuildRejected          (990003, "Demo Tekne Bakım A.Ş.",      "7770001999"),
            BuildSubMerchantCreated(990004, "Demo Balıkçılık Koop.",      "8882221777", "SMK-LIVE-ZP0M4N88", "ACC-990004", "3003"),
        };

        await _db.PaymentProfiles.AddRangeAsync(rows, ct);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Sub-merchant onboarding mock seed complete: {Count} profiles.", rows.Count);
    }

    // SubMerchantCreated: keyed + IBAN → split-eligible, awaiting our verify.
    private static ProviderPaymentProfileEntity BuildSubMerchantCreated(
        long providerId, string legalName, string taxNumber, string subMerchantKey, string accountId, string ibanLast4)
    {
        var p = ProviderPaymentProfileEntity.Create(providerId, "iyzico", legalName, taxNumber);
        p.MarkSubMerchantCreated(subMerchantKey, accountId);
        p.UpdateIban($"enc::demo-iban-{providerId}", ibanLast4);
        return p;
    }

    // Verified: SubMerchantCreated → Verified (VerifiedAt stamped), split-eligible.
    private static ProviderPaymentProfileEntity BuildVerified(
        long providerId, string legalName, string taxNumber, string subMerchantKey, string accountId, string ibanLast4)
    {
        var p = BuildSubMerchantCreated(providerId, legalName, taxNumber, subMerchantKey, accountId, ibanLast4);
        p.MarkVerified();
        return p;
    }

    // Rejected: DataSubmitted → Rejected (no sub-merchant key), not split-eligible.
    private static ProviderPaymentProfileEntity BuildRejected(long providerId, string legalName, string taxNumber)
    {
        var p = ProviderPaymentProfileEntity.Create(providerId, "iyzico", legalName, taxNumber);
        p.SubmitOnboardingData();
        p.UpdateIban($"enc::demo-iban-{providerId}", "9099");
        p.Reject("KYC documents incomplete — tax certificate could not be verified (demo).");
        return p;
    }
}
