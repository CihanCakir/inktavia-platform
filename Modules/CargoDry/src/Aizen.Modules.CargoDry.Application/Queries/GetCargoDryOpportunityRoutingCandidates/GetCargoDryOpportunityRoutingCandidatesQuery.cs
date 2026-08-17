using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOpportunityRoutingCandidates;

/// <summary>
/// Phase 24 — Returns distinct provider profile IDs that have active CargoDry
/// relationships (Active ConsignmentAgreements and/or ProviderInventory records).
///
/// Used by the AdminPanel BFF to resolve candidate providers for the
/// CargoDryOpportunityRouting priority preview (read-only, decision support only).
///
/// Phase 24 hard rules:
/// - Read-only. Does NOT allocate kits, create agreements, or change inventory.
/// - Does NOT trigger scoring or assignment changes.
/// - Sources: Active ConsignmentAgreements + ProviderInventory (primary, non-nullable ProviderProfileId).
/// </summary>
public sealed class GetCargoDryOpportunityRoutingCandidatesQuery
    : AizenQuery<GetCargoDryOpportunityRoutingCandidatesResult> { }

public sealed class GetCargoDryOpportunityRoutingCandidatesResult
{
    /// <summary>Union of distinct provider profile IDs from active agreements + inventory.</summary>
    public IReadOnlyList<long> ProviderProfileIds { get; init; } = [];

    public int AgreementSourceCount  { get; init; }
    public int InventorySourceCount  { get; init; }
    public int TotalUniqueCount      { get; init; }
}
