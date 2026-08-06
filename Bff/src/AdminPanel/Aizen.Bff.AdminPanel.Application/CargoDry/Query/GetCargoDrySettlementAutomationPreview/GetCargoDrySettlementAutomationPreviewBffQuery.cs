using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDrySettlementAutomationPreview;

/// <summary>
/// BFF query for the CargoDry monthly settlement automation dry-run preview.
/// Phase 6 (July 2026).
/// </summary>
public sealed class GetCargoDrySettlementAutomationPreviewBffQuery
    : AizenQuery<GetCargoDrySettlementAutomationPreviewBffQueryResponse>
{
    public int  TargetYearMonth    { get; init; }
    public bool AutoPreparePayment { get; init; } = false;
    public bool AutoPrepareInvoice { get; init; } = false;
}

public sealed class GetCargoDrySettlementAutomationPreviewBffQueryResponse
{
    public CargoDrySettlementAutomationPreviewBffDto Preview { get; init; } = default!;
}
