namespace Aizen.Modules.Payment.Abstraction.Interface;

/// <summary>
/// Cross-module boundary service: allows CargoDry.Application to query active
/// CommissionRule records scoped to the CargoDry context (ContextType = CargoDry)
/// without taking a direct dependency on Payment.Repository or Payment.Application.
///
/// Registered in Payment.Application/DependencyInjection.cs.
/// Consumed by CargoDryCommercialRuleResolver in CargoDry.Application.
/// Phase 5 (July 2026).
/// </summary>
public interface ICargoDryCommissionRuleLookupService
{
    /// <summary>
    /// Finds the highest-priority active CargoDry CommissionRule scoped to a specific provider.
    /// Filters: ContextType = CargoDry, RuleType = ProviderOverride, ProviderProfileId = provider,
    ///          IsEffective at effectiveAtUtc, optional CurrencyCode match.
    /// Returns the first match ordered by EffectiveFrom descending (most recent wins).
    /// Returns null if no matching rule exists.
    /// </summary>
    Task<CargoDryCommissionRuleLookupResult?> FindProviderSpecificRuleAsync(
        long      providerProfileId,
        string?   currencyCode,
        DateTime  effectiveAtUtc,
        CancellationToken ct);

    /// <summary>
    /// Finds the highest-priority active CargoDry CommissionRule scoped to a product+channel combination.
    /// Filters: ContextType = CargoDry, ProductCode = product, SalesChannel = channel (as int),
    ///          IsEffective at effectiveAtUtc, optional CurrencyCode match.
    /// Returns the first match ordered by EffectiveFrom descending.
    /// Returns null if no matching rule exists.
    ///
    /// salesChannelValue is passed as int to avoid cross-module enum coupling.
    /// Payment.Abstraction.Enum.SalesChannel and CargoDry.Abstraction.Enum.SalesChannel
    /// share identical integer values by design.
    /// </summary>
    Task<CargoDryCommissionRuleLookupResult?> FindProductChannelRuleAsync(
        string    productCode,
        int       salesChannelValue,
        string?   currencyCode,
        DateTime  effectiveAtUtc,
        CancellationToken ct);
}

/// <summary>
/// Flat result DTO returned by ICargoDryCommissionRuleLookupService.
/// Contains only the fields needed by the CargoDry commercial rule resolver.
/// Phase 5 (July 2026).
/// </summary>
public sealed class CargoDryCommissionRuleLookupResult
{
    public long    RuleId         { get; init; }
    public string? RuleName       { get; init; }
    public string? RuleCode       { get; init; }
    public decimal CommissionRate { get; init; }
    public string? CurrencyCode   { get; init; }
    public string? Notes          { get; init; }
}
