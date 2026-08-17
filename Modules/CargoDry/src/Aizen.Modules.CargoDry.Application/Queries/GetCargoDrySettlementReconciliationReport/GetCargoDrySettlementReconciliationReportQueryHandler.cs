using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDrySettlementReconciliationReport;

[DocumentationInfo("Get CargoDry settlement reconciliation report query handler",
    "Returns a paged settlement-payout reconciliation report for admin finance auditing. " +
    "Loads settlements from the CargoDry module and computes mismatch flags server-side " +
    "from entity field inspection only. No cross-module calls are made. " +
    "Supports filtering by provider, product, settlement status, and date range. " +
    "Phase 15 (July 2026).")]
public sealed class GetCargoDrySettlementReconciliationReportQueryHandler
    : AizenQueryHandler<GetCargoDrySettlementReconciliationReportQuery, GetCargoDrySettlementReconciliationReportResponse>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;

    public GetCargoDrySettlementReconciliationReportQueryHandler(
        ICargoDrySellThroughSettlementRepository settlements)
        => _settlements = settlements;

    public override async Task<GetCargoDrySettlementReconciliationReportResponse> Handle(
        GetCargoDrySettlementReconciliationReportQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        // Load matching settlements — no search param, consignment agreement filter not needed
        // for reconciliation report.
        var (items, total) = await _settlements.GetPagedAsync(
            request.providerProfileId,
            consignmentAgreementId: null,
            request.ProductCode,
            request.Status,
            periodFrom: request.DateFrom,
            periodTo:   request.DateTo,
            search:     null,
            skip:       0,                    // load all matching for mismatch filtering
            take:       int.MaxValue,
            ct);

        // Compute mismatch flags server-side
        var rows = items
            .Select(s =>
            {
                var flags    = new List<string>();
                var warnings = new List<string>();

                // ── Mismatch: settlement is ReadyForSettlement but no payout record was created
                if (s.Status == CargoDrySellThroughSettlementStatus.ReadyForSettlement
                    && !s.PayoutRecordId.HasValue)
                    flags.Add("SettlementReadyButNoPayoutRecord");

                // ── Mismatch: settlement is Scheduled but no invoice has been prepared
                if (s.Status == CargoDrySellThroughSettlementStatus.Scheduled
                    && !s.InvoiceId.HasValue)
                    flags.Add("ScheduledButNoInvoice");

                // ── Mismatch: payout completion recorded but settlement not yet closed as Settled
                if (s.PayoutCompletedAtUtc.HasValue
                    && s.Status != CargoDrySellThroughSettlementStatus.Settled)
                    flags.Add("PayoutCompletedButSettlementNotSettled");

                // ── Mismatch: settlement has no currency code
                if (string.IsNullOrWhiteSpace(s.CurrencyCode))
                    flags.Add("MissingCurrency");

                // ── Warning: settlement amounts are zero but not Cancelled
                if (s.TotalSaleAmount == 0m
                    && s.Status != CargoDrySellThroughSettlementStatus.Cancelled)
                    warnings.Add("ZeroTotalSaleAmount");

                // ── Warning: settlement is Disputed with no PayoutRecordId
                if (s.Status == CargoDrySellThroughSettlementStatus.Disputed
                    && !s.PayoutRecordId.HasValue)
                    warnings.Add("DisputedWithoutPayoutRecord");

                return new CargoDrySettlementReconciliationRowDto
                {
                    SettlementId         = s.Id,
                    SettlementCode       = s.SettlementCode,
                    ProviderProfileId    = s.ProviderProfileId,
                    ProductCode          = s.ProductCode,
                    CurrencyCode         = s.CurrencyCode ?? string.Empty,
                    SettlementStatus     = s.Status.ToString(),
                    AttributionCount     = s.TotalKitCount,
                    TotalSaleAmount      = s.TotalSaleAmount,
                    ProviderPayoutAmount = s.ProviderPayoutAmount,
                    PlatformShareAmount  = s.TotalSaleAmount - s.ProviderPayoutAmount,
                    PayoutRecordId       = s.PayoutRecordId,
                    PayoutStatus         = null,          // cross-module — not enriched here
                    InvoiceId            = s.InvoiceId,
                    InvoiceStatus        = null,          // cross-module — not enriched here
                    PaymentPreparedAtUtc = s.PaymentPreparedAtUtc,
                    InvoicePreparedAtUtc = s.InvoicePreparedAtUtc,
                    PayoutCompletedAtUtc = s.PayoutCompletedAtUtc,
                    CreatedAtUtc         = s.CreatedAtUtc,
                    MismatchFlags        = flags,
                    Warnings             = warnings,
                };
            })
            .ToList();

        // Apply hasMismatches filter in-memory after computation
        if (request.HasMismatches == true)
            rows = rows.Where(r => r.MismatchFlags.Count > 0).ToList();
        else if (request.HasMismatches == false)
            rows = rows.Where(r => r.MismatchFlags.Count == 0).ToList();

        var mismatchCount = rows.Count(r => r.MismatchFlags.Count > 0);

        // Paginate after filtering
        var effectiveTotal = rows.Count;
        var pagedItems     = rows.Skip(skip).Take(request.PageSize).ToList();

        return new GetCargoDrySettlementReconciliationReportResponse
        {
            Report = new CargoDrySettlementReconciliationReportDto
            {
                Items         = pagedItems,
                Total         = effectiveTotal,
                Page          = request.Page,
                PageSize      = request.PageSize,
                MismatchCount = mismatchCount,
            }
        };
    }
}
