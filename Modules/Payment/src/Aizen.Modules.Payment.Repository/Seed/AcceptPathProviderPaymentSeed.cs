using Aizen.Modules.Payment.Domain.Entities.PaymentProfile;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// Makes the "owner accepts a provider's offer → payment economics" happy path work on a freshly-seeded DB with NO
/// manual edits. The three providers that actually submit offers in the ServiceRequest mock seed — profile ids
/// <b>11011 / 11012 / 11013</b> (marina.ops / teknik.servis / cargodry.team) — had no <c>provider_payment_profiles</c>
/// row, so acceptance was blocked at the split-eligibility gate. This seeder gives each a <b>Verified</b> (keyed +
/// IBAN → split-eligible) profile and a <b>clean ₺0</b> balance so a normal accept clears.
///
/// <para>Deliberately does NOT touch profile <b>100011</b> ("PROVIDER 2 AS"): that id is owned by
/// <see cref="Provider2PositiveBranchMockSeed"/>, which intentionally drives it OVER its negative-balance limit to light
/// up the Wave-B finance "danger strip" demo. We only ADD a Verified profile for 100011 (it had none) so it is
/// split-eligible for other flows; its Wave-B over-limit balance is left as the demo owns it. The clean accept path uses
/// 11011/11012/11013, not 100011.</para>
///
/// <para>Idempotent (find-or-create per provider id) and dev/local only. Uses the domain factories
/// (<see cref="ProviderPaymentProfileEntity"/> / <see cref="ProviderBalanceEntity"/>) so invariants hold — same pattern
/// as <see cref="SubMerchantOnboardingMockSeed"/>.</para>
/// </summary>
public sealed class AcceptPathProviderPaymentSeed
{
    private const string CurrencyTry = "TRY";

    // The providers that submit offers in the SR mock seed (real Identity profiles 11011/11012/11013).
    private static readonly (long ProfileId, string LegalName, string TaxNumber, string SubMerchantKey, string AccountId, string IbanLast4)[] OfferProviders =
    {
        (11011, "Marina Ops A.Ş.",        "5110110011", "SMK-DEV-11011", "ACC-11011", "1011"),
        (11012, "Teknik Servis Ltd.",     "5110120012", "SMK-DEV-11012", "ACC-11012", "1012"),
        (11013, "CargoDry Team A.Ş.",     "5110130013", "SMK-DEV-11013", "ACC-11013", "1013"),
    };

    // 100011 = "PROVIDER 2 AS" — only ADD a Verified profile (Wave-B owns its balance; do not seed/override a balance).
    private const long Provider2ProfileId = 100011;

    private readonly PaymentDbContext _db;
    private readonly ILogger<AcceptPathProviderPaymentSeed> _logger;

    public AcceptPathProviderPaymentSeed(PaymentDbContext db, ILogger<AcceptPathProviderPaymentSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var added = 0;

        foreach (var p in OfferProviders)
        {
            if (await EnsureVerifiedProfileAsync(p.ProfileId, p.LegalName, p.TaxNumber, p.SubMerchantKey, p.AccountId, p.IbanLast4, ct))
                added++;
            await EnsureCleanBalanceAsync(p.ProfileId, ct);
        }

        // 100011: profile only (split-eligible); leave the Wave-B over-limit balance to its own seeder.
        if (await EnsureVerifiedProfileAsync(
                Provider2ProfileId, "PROVIDER 2 AS", "1000000011", "SMK-DEV-100011", "ACC-100011", "2011", ct))
            added++;

        if (added > 0)
            await _db.SaveChangesAsync(ct);

        _logger.LogInformation("AcceptPathProviderPaymentSeed complete: {Added} Verified profile(s) ensured + clean balances.", added);
    }

    /// <summary>Find-or-create a Verified, keyed, IBAN-present profile. Returns true if a new row was added.</summary>
    private async Task<bool> EnsureVerifiedProfileAsync(
        long providerProfileId, string legalName, string taxNumber,
        string subMerchantKey, string accountId, string ibanLast4, CancellationToken ct)
    {
        if (await _db.PaymentProfiles.AnyAsync(x => x.ProviderProfileId == providerProfileId, ct))
            return false;

        var profile = ProviderPaymentProfileEntity.Create(providerProfileId, "iyzico", legalName, taxNumber);
        profile.MarkSubMerchantCreated(subMerchantKey, accountId);
        profile.UpdateIban($"enc::dev-iban-{providerProfileId}", ibanLast4);
        profile.MarkVerified();

        await _db.PaymentProfiles.AddAsync(profile, ct);
        return true;
    }

    /// <summary>Ensure a clean ₺0 balance row exists (absent ⇒ create at 0). Never forces an existing row negative.</summary>
    private async Task EnsureCleanBalanceAsync(long providerProfileId, CancellationToken ct)
    {
        var exists = await _db.ProviderBalances
            .AnyAsync(b => b.ProviderProfileId == providerProfileId && b.CurrencyCode == CurrencyTry, ct);
        if (exists) return;

        var balance = ProviderBalanceEntity.Create(providerProfileId, CurrencyTry, negativeBalanceLimit: 5_000m);
        await _db.ProviderBalances.AddAsync(balance, ct);
    }
}
