using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDrySettlementInvoicePreparationPreview;

/// <summary>
/// BFF query that proxies to the CargoDry commercial module's invoice-preparation-preview endpoint.
/// Returns eligibility status, blocking reasons, financial summary, and existing invoice links.
/// Never throws for business ineligibility — returns CanPrepare=false + BlockingReasons instead.
/// Phase 4C (July 2026).
/// </summary>
public sealed class GetCargoDrySettlementInvoicePreparationPreviewBffQuery
    : AizenQuery<GetCargoDrySettlementInvoicePreparationPreviewBffQueryResponse>
{
    public long SettlementId { get; init; }
}

public sealed class GetCargoDrySettlementInvoicePreparationPreviewBffQueryResponse
{
    public CargoDrySettlementInvoicePreparationPreviewBffDto? Preview { get; init; }
}
