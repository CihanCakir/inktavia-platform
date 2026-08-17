using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOpportunityRoutingCandidates;

/// <summary>
/// Phase 24 — Resolves distinct CargoDry provider profile IDs for the opportunity
/// routing priority preview.
///
/// Sources (both non-nullable ProviderProfileId):
///   1. Active ConsignmentAgreements (providers under active consignment)
///   2. ProviderInventory records (providers with any inventory allocation)
///
/// The union is deduplicated. Used by the AdminPanel BFF to resolve Profile engine
/// candidates before calling the priority preview with CargoDryOpportunityRouting context.
///
/// Phase 24 hard rules enforced here:
///   - Read-only. No kit allocation, no agreement/inventory changes. (Rules 1–4)
///   - No settlement/payout/commission changes. (Rules 5–8)
///   - No provider blocking or enforcement. (Rules 9–11)
/// </summary>
[DocumentationInfo("GetCargoDryOpportunityRoutingCandidatesQueryHandler",
    "Phase 24: Returns union of distinct provider profile IDs from " +
    "Active ConsignmentAgreements + ProviderInventory. " +
    "Read-only candidate resolution for CargoDryOpportunityRouting priority preview. " +
    "No writes, no scoring, no enforcement.")]
public sealed class GetCargoDryOpportunityRoutingCandidatesQueryHandler
    : AizenQueryHandler<GetCargoDryOpportunityRoutingCandidatesQuery, GetCargoDryOpportunityRoutingCandidatesResult>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;
    private readonly ICargoDryProviderInventoryRepository   _inventory;

    public GetCargoDryOpportunityRoutingCandidatesQueryHandler(
        ICargoDryConsignmentAgreementRepository agreements,
        ICargoDryProviderInventoryRepository   inventory)
    {
        _agreements = agreements;
        _inventory  = inventory;
    }

    public override async Task<GetCargoDryOpportunityRoutingCandidatesResult> Handle(
        GetCargoDryOpportunityRoutingCandidatesQuery request, CancellationToken ct)
    {
        // ── Parallel fetch: active agreement IDs + inventory IDs ───────────────
        var agreementIdsTask = _agreements.GetDistinctActiveProviderProfileIdsAsync(ct);
        var inventoryIdsTask = _inventory.GetDistinctProviderProfileIdsAsync(ct);

        await Task.WhenAll(agreementIdsTask, inventoryIdsTask);

        var agreementIds = agreementIdsTask.Result;
        var inventoryIds = inventoryIdsTask.Result;

        // ── Union and deduplicate ──────────────────────────────────────────────
        var union = agreementIds
            .Union(inventoryIds)
            .Distinct()
            .ToList();

        return new GetCargoDryOpportunityRoutingCandidatesResult
        {
            ProviderProfileIds  = union,
            AgreementSourceCount = agreementIds.Count,
            InventorySourceCount = inventoryIds.Count,
            TotalUniqueCount    = union.Count,
        };
    }
}
