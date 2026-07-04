using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Aizen.Modules.Payment.Abstraction.Interface;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Implements the 7-tier CargoDry commercial rule cascade.
///
/// Resolution priority (highest → lowest):
///   Tier 0 — DirectSale / ProviderResale short-circuit → rate=0, source=DirectSaleNoProviderShare
///   Tier 1 — AdminOverrideRate present in request
///   Tier 2 — Active provider-specific CargoDry CommissionRule (Payment module lookup)
///   Tier 3 — Active product + sales-channel CargoDry CommissionRule (Payment module lookup)
///   Tier 4 — ConsignmentAgreement.ConsignmentRate (ConsignmentSellThrough only)
///   Tier 5 — CargoDryProduct.ProviderCommissionRate (product default)
///   Tier 6 — Unresolved → CanResolve = false, blocking reason returned
///
/// Hard constraints enforced here:
///   • Never invents a rate.
///   • Never silently falls back to 0 for provider payout models.
///   • DirectSale resolves to 0 only via the explicit short-circuit (no provider share).
///
/// Phase 5 (July 2026): CargoDry Commercial Rule Resolver &amp; Commission/Rate Engine.
/// </summary>
[DocumentationInfo("CargoDry Commercial Rule Resolver",
    "Evaluates the 7-tier commission rate cascade for a CargoDry kit sale and returns a " +
    "fully-traceable resolution result. Used by ResolveCargoDrySalesAttributionFinancialsCommandHandler " +
    "and preview query handlers. Phase 5 (July 2026).")]
public sealed class CargoDryCommercialRuleResolver : ICargoDryCommercialRuleResolver
{
    private readonly ICargoDryProductRepository             _productRepo;
    private readonly ICargoDryConsignmentAgreementRepository _agreementRepo;
    private readonly ICargoDryCommissionRuleLookupService   _ruleLookup;

    public CargoDryCommercialRuleResolver(
        ICargoDryProductRepository             productRepo,
        ICargoDryConsignmentAgreementRepository agreementRepo,
        ICargoDryCommissionRuleLookupService    ruleLookup)
    {
        _productRepo   = productRepo;
        _agreementRepo = agreementRepo;
        _ruleLookup    = ruleLookup;
    }

    /// <inheritdoc/>
    public async Task<CargoDryCommercialRuleResolutionResult> ResolveAsync(
        CargoDryCommercialRuleResolutionRequest request,
        CancellationToken ct)
    {
        // ── Tier 0: DirectSale / ProviderResale — no provider share, short-circuit ──────
        if (request.SalesChannel is SalesChannel.DirectSale or SalesChannel.ProviderResale)
        {
            return BuildResult(
                canResolve:  true,
                rate:        0m,
                ruleSource:  RuleSource.DirectSaleNoProviderShare,
                currencyCode: request.CurrencyCode,
                salePrice:   request.SalePrice);
        }

        var warnings = new List<string>();

        // ── Tier 1: Admin override rate ──────────────────────────────────────────────────
        if (request.AdminOverrideRate.HasValue)
        {
            ValidateRate(request.AdminOverrideRate.Value, nameof(request.AdminOverrideRate));

            return BuildResult(
                canResolve:  true,
                rate:        request.AdminOverrideRate.Value,
                ruleSource:  RuleSource.AdminOverride,
                currencyCode: request.CurrencyCode,
                salePrice:   request.SalePrice);
        }

        // ── Tier 2: Provider-specific CargoDry CommissionRule ────────────────────────────
        if (request.ProviderProfileId.HasValue)
        {
            var providerRule = await _ruleLookup.FindProviderSpecificRuleAsync(
                request.ProviderProfileId.Value,
                request.CurrencyCode,
                request.EffectiveAtUtc,
                ct);

            if (providerRule is not null)
            {
                return BuildResult(
                    canResolve:  true,
                    rate:        providerRule.CommissionRate,
                    ruleSource:  RuleSource.CommissionRuleProviderSpecific,
                    currencyCode: providerRule.CurrencyCode ?? request.CurrencyCode,
                    salePrice:   request.SalePrice,
                    ruleId:      providerRule.RuleId,
                    ruleName:    providerRule.RuleName);
            }
        }

        // ── Tier 3: Product + sales-channel CargoDry CommissionRule ─────────────────────
        var productChannelRule = await _ruleLookup.FindProductChannelRuleAsync(
            request.ProductCode,
            (int)request.SalesChannel,
            request.CurrencyCode,
            request.EffectiveAtUtc,
            ct);

        if (productChannelRule is not null)
        {
            return BuildResult(
                canResolve:  true,
                rate:        productChannelRule.CommissionRate,
                ruleSource:  RuleSource.CommissionRuleProductChannel,
                currencyCode: productChannelRule.CurrencyCode ?? request.CurrencyCode,
                salePrice:   request.SalePrice,
                ruleId:      productChannelRule.RuleId,
                ruleName:    productChannelRule.RuleName);
        }

        // ── Tier 4: ConsignmentAgreement.ConsignmentRate ─────────────────────────────────
        if (request.SalesChannel == SalesChannel.ConsignmentSellThrough
            && request.ConsignmentAgreementId.HasValue)
        {
            var agreement = await _agreementRepo.GetByIdAsync(
                request.ConsignmentAgreementId.Value, ct);

            if (agreement is not null && agreement.ConsignmentRate > 0m)
            {
                return BuildResult(
                    canResolve:  true,
                    rate:        agreement.ConsignmentRate,
                    ruleSource:  RuleSource.ConsignmentAgreement,
                    currencyCode: request.CurrencyCode,
                    salePrice:   request.SalePrice);
            }

            if (agreement is not null && agreement.ConsignmentRate == 0m)
            {
                warnings.Add(
                    $"ConsignmentAgreement {request.ConsignmentAgreementId} found but has ConsignmentRate=0 " +
                    "— falling through to product default.");
            }
            else if (agreement is null)
            {
                warnings.Add(
                    $"ConsignmentAgreementId {request.ConsignmentAgreementId} not found — " +
                    "falling through to product default.");
            }
        }

        // ── Tier 5: CargoDryProduct.ProviderCommissionRate ───────────────────────────────
        var product = await _productRepo.GetByCodeAsync(request.ProductCode, ct);

        if (product is not null && product.ProviderCommissionRate.HasValue)
        {
            return BuildResult(
                canResolve:  true,
                rate:        product.ProviderCommissionRate.Value,
                ruleSource:  RuleSource.CargoDryProductDefault,
                currencyCode: request.CurrencyCode,
                salePrice:   request.SalePrice,
                warnings:    warnings);
        }

        if (product is null)
        {
            warnings.Add($"CargoDry product '{request.ProductCode}' not found in catalog.");
        }
        else
        {
            warnings.Add($"Product '{request.ProductCode}' has no ProviderCommissionRate configured.");
        }

        // ── Tier 6: Unresolved ───────────────────────────────────────────────────────────
        return new CargoDryCommercialRuleResolutionResult
        {
            CanResolve      = false,
            RuleSource      = RuleSource.Unresolved,
            CurrencyCode    = request.CurrencyCode,
            SalePrice       = request.SalePrice,
            BlockingReasons = new[]
            {
                $"No commission rule found for product '{request.ProductCode}', " +
                $"channel '{request.SalesChannel}', currency '{request.CurrencyCode}'. " +
                "Attribution requires manual commercial review."
            },
            Warnings = warnings,
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────────

    private static CargoDryCommercialRuleResolutionResult BuildResult(
        bool           canResolve,
        decimal        rate,
        string         ruleSource,
        string?        currencyCode,
        decimal?       salePrice,
        long?          ruleId    = null,
        string?        ruleName  = null,
        IList<string>? warnings  = null)
    {
        decimal? providerShare = null;
        decimal? platformShare = null;

        if (salePrice.HasValue && canResolve)
        {
            providerShare = Math.Round(salePrice.Value * rate,                  4, MidpointRounding.AwayFromZero);
            platformShare = Math.Round(salePrice.Value - providerShare.Value,   4, MidpointRounding.AwayFromZero);
        }

        return new CargoDryCommercialRuleResolutionResult
        {
            CanResolve           = canResolve,
            ResolvedRate         = rate,
            CurrencyCode         = currencyCode,
            RuleSource           = ruleSource,
            RuleId               = ruleId,
            RuleName             = ruleName,
            SalePrice            = salePrice,
            ProviderShareAmount  = providerShare,
            PlatformShareAmount  = platformShare,
            Warnings             = (IReadOnlyList<string>?)warnings ?? Array.Empty<string>(),
        };
    }

    private static void ValidateRate(decimal rate, string paramName)
    {
        if (rate is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(paramName,
                $"Commission rate must be between 0.00 and 1.00 (received {rate}).");
    }

    // ── RuleSource constants ─────────────────────────────────────────────────────────────
    private static class RuleSource
    {
        public const string AdminOverride                 = "AdminOverride";
        public const string CommissionRuleProviderSpecific = "CommissionRuleProviderSpecific";
        public const string CommissionRuleProductChannel   = "CommissionRuleProductChannel";
        public const string ConsignmentAgreement          = "ConsignmentAgreement";
        public const string CargoDryProductDefault        = "CargoDryProductDefault";
        public const string DirectSaleNoProviderShare     = "DirectSaleNoProviderShare";
        public const string Unresolved                    = "Unresolved";
    }
}
