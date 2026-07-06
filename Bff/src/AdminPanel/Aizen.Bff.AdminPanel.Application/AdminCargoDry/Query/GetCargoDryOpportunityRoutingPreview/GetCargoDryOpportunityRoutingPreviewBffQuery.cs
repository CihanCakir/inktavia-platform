using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryOpportunityRoutingPreview;

/// <summary>
/// Phase 24 — BFF orchestration query: CargoDry opportunity routing priority preview.
///
/// The handler:
///   1. Calls the CargoDry module to resolve distinct provider profile IDs from
///      Active ConsignmentAgreements + ProviderInventory (union, deduplicated).
///   2. Optionally uses the admin's CandidateProfileIds override instead.
///   3. Calls the Profile performance priority-preview engine with context
///      "CargoDryOpportunityRouting".
///   4. Post-filters Flagged providers when IncludeFlagged == false.
///
/// Decision-support only — no allocation, no agreement changes, no enforcement.
/// </summary>
public sealed class GetCargoDryOpportunityRoutingPreviewBffQuery
    : AizenQuery<CargoDryOpportunityRoutingPreviewBffResponse>
{
    public CargoDryOpportunityRoutingPreviewBffRequest RequestBody { get; }

    public GetCargoDryOpportunityRoutingPreviewBffQuery(
        CargoDryOpportunityRoutingPreviewBffRequest requestBody)
    {
        RequestBody = requestBody;
    }
}
