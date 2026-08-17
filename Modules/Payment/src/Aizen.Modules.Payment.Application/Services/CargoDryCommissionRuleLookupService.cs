using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Abstraction.Interface;
using Aizen.Modules.Payment.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Payment.Application.Services;

/// <summary>
/// Concrete implementation of ICargoDryCommissionRuleLookupService.
/// Queries commission_rules where ContextType = CargoDry (20).
/// Only returns Active rules that are effective at the requested date.
/// Phase 5 (July 2026).
/// </summary>
public sealed class CargoDryCommissionRuleLookupService : ICargoDryCommissionRuleLookupService
{
    private readonly PaymentDbContext _db;

    public CargoDryCommissionRuleLookupService(PaymentDbContext db) => _db = db;

    /// <inheritdoc/>
    public async Task<CargoDryCommissionRuleLookupResult?> FindProviderSpecificRuleAsync(
        long      providerProfileId,
        string?   currencyCode,
        DateTime  effectiveAtUtc,
        CancellationToken ct)
    {
        var utcAt = DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc);

        var query = _db.CommissionRules
            .Where(r => r.IsActive
                     && r.ContextType  == TransactionContextType.CargoDry
                     && r.RuleType     == CommissionRuleType.ProviderOverride
                     && r.ProviderProfileId == providerProfileId
                     && r.EffectiveFrom <= utcAt
                     && (r.EffectiveTo == null || r.EffectiveTo >= utcAt));

        // Narrow to currency if supplied; null on rule = any currency
        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            var upper = currencyCode.ToUpperInvariant();
            query = query.Where(r => r.CurrencyCode == null || r.CurrencyCode == upper);
        }

        var rule = await query
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (rule is null) return null;

        return new CargoDryCommissionRuleLookupResult
        {
            RuleId         = rule.Id,
            RuleName       = rule.RuleName,
            RuleCode       = rule.RuleCode,
            CommissionRate = rule.CommissionRate,
            CurrencyCode   = rule.CurrencyCode,
            Notes          = rule.Notes,
        };
    }

    /// <inheritdoc/>
    public async Task<CargoDryCommissionRuleLookupResult?> FindProductChannelRuleAsync(
        string    productCode,
        int       salesChannelValue,
        string?   currencyCode,
        DateTime  effectiveAtUtc,
        CancellationToken ct)
    {
        var utcAt   = DateTime.SpecifyKind(effectiveAtUtc, DateTimeKind.Utc);
        var channel = (SalesChannel)salesChannelValue;
        var code    = productCode.ToUpperInvariant();

        var query = _db.CommissionRules
            .Where(r => r.IsActive
                     && r.ContextType == TransactionContextType.CargoDry
                     && r.ProductCode  == code
                     && r.SalesChannel == channel
                     && r.EffectiveFrom <= utcAt
                     && (r.EffectiveTo == null || r.EffectiveTo >= utcAt));

        if (!string.IsNullOrWhiteSpace(currencyCode))
        {
            var upper = currencyCode.ToUpperInvariant();
            query = query.Where(r => r.CurrencyCode == null || r.CurrencyCode == upper);
        }

        var rule = await query
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (rule is null) return null;

        return new CargoDryCommissionRuleLookupResult
        {
            RuleId         = rule.Id,
            RuleName       = rule.RuleName,
            RuleCode       = rule.RuleCode,
            CommissionRate = rule.CommissionRate,
            CurrencyCode   = rule.CurrencyCode,
            Notes          = rule.Notes,
        };
    }
}
