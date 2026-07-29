using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Domain.Entities.RefundAllocation;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Payment.Repository.Seed;

/// <summary>
/// BE-P10 §7.2 — seeds the default refund-allocation policy (TRY). Idempotent (keyed by <c>PolicyCode == null &amp;&amp;
/// CurrencyCode == "TRY"</c>). All modes are admin-tunable; this is the MVP launch table.
/// </summary>
public sealed class RefundAllocationPolicySeed
{
    private readonly PaymentDbContext _db;
    private readonly ILogger<RefundAllocationPolicySeed> _logger;
    public RefundAllocationPolicySeed(PaymentDbContext db, ILogger<RefundAllocationPolicySeed> logger) { _db = db; _logger = logger; }

    private const string Currency = "TRY";
    private const decimal DefaultNegativeBalanceLimit = 50000m;   // admin-tunable placeholder

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existing = await _db.RefundAllocationPolicies
            .FirstOrDefaultAsync(x => x.PolicyCode == null && x.CurrencyCode == Currency, ct);
        if (existing is not null) { _logger.LogDebug("Refund-allocation policy seed skipped (already present)."); return; }

        var policy = RefundAllocationPolicyEntity.Create(
            currencyCode: Currency, negativeBalanceLimit: DefaultNegativeBalanceLimit,
            effectiveFrom: DateTime.UtcNow, effectiveTo: null,
            policyCode: null,   // seed-owned rows identified by PolicyCode IS NULL
            policyName: "Default refund-allocation policy (MVP §7.2)",
            notes: "Launch placeholder — per-cause platform-fee refund modes + negative-balance limit are admin-tunable.");

        // §7.2 default table
        policy.AddRule(RefundCause.ProviderCancelled,                 PlatformFeeRefundMode.Full);
        policy.AddRule(RefundCause.TechnicalFailure,                  PlatformFeeRefundMode.Full);
        policy.AddRule(RefundCause.DuplicatePayment,                  PlatformFeeRefundMode.Full);
        policy.AddRule(RefundCause.CustomerCancelledBeforeWork,       PlatformFeeRefundMode.Full);
        policy.AddRule(RefundCause.CustomerCancelledAfterWorkStarted, PlatformFeeRefundMode.RuleBased);   // MVP → pro-rata
        policy.AddRule(RefundCause.DisputeCustomerFavoured,           PlatformFeeRefundMode.Full);
        policy.AddRule(RefundCause.DisputeProviderFavoured,           PlatformFeeRefundMode.None);
        policy.AddRule(RefundCause.AdministrativeCorrection,          PlatformFeeRefundMode.Full);

        await _db.RefundAllocationPolicies.AddAsync(policy, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded default refund-allocation policy (TRY, MVP §7.2 table).");
    }
}
