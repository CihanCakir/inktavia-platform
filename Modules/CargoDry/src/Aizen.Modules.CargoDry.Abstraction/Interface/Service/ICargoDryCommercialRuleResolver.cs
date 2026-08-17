using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Abstraction.Interface.Service;

/// <summary>
/// Resolves the applicable commission rate for a CargoDry kit sale by evaluating
/// a prioritised cascade of rule tiers.
///
/// Resolution priority (highest → lowest):
///   1. Admin override rate from the command (<see cref="CargoDryCommercialRuleResolutionRequest.AdminOverrideRate"/>)
///   2. Active provider-specific CargoDry CommissionRule
///   3. Active product + sales-channel CargoDry CommissionRule
///   4. Active ConsignmentAgreement.ConsignmentRate (ConsignmentSellThrough only)
///   5. CargoDryProduct.ProviderCommissionRate (product default)
///   6. No rule found → CanResolve = false, blocking reason returned
///
/// Hard constraints:
///   - Never invents a rate.
///   - Never silently falls back to 0 for provider payout models.
///   - DirectSale / ProviderResale short-circuit immediately to RuleSource = DirectSaleNoProviderShare, rate = 0.
///
/// Phase 5 (July 2026): CargoDry Commercial Rule Resolver & Commission/Rate Engine.
/// </summary>
public interface ICargoDryCommercialRuleResolver
{
    /// <summary>
    /// Evaluates the rule cascade for the given <paramref name="request"/> and returns a
    /// fully-populated <see cref="CargoDryCommercialRuleResolutionResult"/>.
    /// </summary>
    Task<CargoDryCommercialRuleResolutionResult> ResolveAsync(
        CargoDryCommercialRuleResolutionRequest request,
        CancellationToken ct);
}
