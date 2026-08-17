using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySettlementPaymentPreparationPreview;

/// <summary>
/// BFF query that proxies to the CargoDry commercial module's payment-preparation-preview endpoint.
/// Returns eligibility status, blocking reasons, attribution counts, and existing payout links.
/// Never throws for business ineligibility — returns CanPrepare=false + BlockingReasons instead.
/// Phase 4B (July 2026).
/// </summary>
public sealed class GetCargoDrySettlementPaymentPreparationPreviewBffQuery
    : AizenQuery<GetCargoDrySettlementPaymentPreparationPreviewBffQueryResponse>
{
    public long SettlementId { get; init; }
}

public sealed class GetCargoDrySettlementPaymentPreparationPreviewBffQueryResponse
{
    public CargoDrySettlementPaymentPreparationPreviewBffDto? Preview { get; init; }
}
