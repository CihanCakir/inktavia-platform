using Aizen.Modules.Payment.Abstraction.Interface;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Null-object fallback for <see cref="ICargoDryCommissionRuleLookupService"/> used when the CargoDry module
/// is hosted WITHOUT Payment.Application (split-service deployment: aizen-cargodry has no Payment DI, so the
/// Phase 5 cross-module registration is absent and <see cref="CargoDryCommercialRuleResolver"/> could not be
/// activated — Supply v2 E2E root cause, Sep 2026).
///
/// Both lookups return null, which degrades the resolver exactly to its designed fallback tiers:
/// Tier 4 (ConsignmentAgreement.ConsignmentRate) for consignment sell-through, then product defaults.
/// Registered with TryAdd, so a co-hosted Payment.Application registration (single-host monolith) always wins.
/// </summary>
public sealed class NullCargoDryCommissionRuleLookupService : ICargoDryCommissionRuleLookupService
{
    public Task<CargoDryCommissionRuleLookupResult?> FindProviderSpecificRuleAsync(
        long providerProfileId, string? currencyCode, DateTime effectiveAtUtc, CancellationToken ct)
        => Task.FromResult<CargoDryCommissionRuleLookupResult?>(null);

    public Task<CargoDryCommissionRuleLookupResult?> FindProductChannelRuleAsync(
        string productCode, int salesChannelValue, string? currencyCode, DateTime effectiveAtUtc, CancellationToken ct)
        => Task.FromResult<CargoDryCommissionRuleLookupResult?>(null);
}
