using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalReconciliationReport;

[DocumentationInfo("Get CargoDry renewal reconciliation report query handler",
    "Returns a paged renewal billing reconciliation report for admin finance auditing. " +
    "Loads renewal preparations from the CargoDry module and computes mismatch flags " +
    "server-side from entity field inspection only. No cross-module calls are made. " +
    "Supports filtering by product, owner, vessel, status, and date range. " +
    "Phase 15 (July 2026).")]
public sealed class GetCargoDryRenewalReconciliationReportQueryHandler
    : AizenQueryHandler<GetCargoDryRenewalReconciliationReportQuery, GetCargoDryRenewalReconciliationReportResponse>
{
    private readonly ICargoDryRenewalPreparationRepository _renewals;

    public GetCargoDryRenewalReconciliationReportQueryHandler(
        ICargoDryRenewalPreparationRepository renewals)
        => _renewals = renewals;

    public override async Task<GetCargoDryRenewalReconciliationReportResponse> Handle(
        GetCargoDryRenewalReconciliationReportQuery request, CancellationToken ct)
    {
        // Load all matching renewals — paginate after mismatch filtering
        var (items, _) = await _renewals.GetPagedAsync(
            kitId:              null,
            kitCode:            null,
            productCode:        request.ProductCode,
            ownerUserId:        request.OwnerUserId,
            vesselId:           request.VesselId,
            status:             request.Status,
            notificationStatus: request.NotificationStatus,
            preparedFrom:       request.DateFrom,
            preparedTo:         request.DateTo,
            skip:               0,
            take:               int.MaxValue,
            ct);

        // Compute mismatch flags server-side
        var rows = items
            .Select(r =>
            {
                var flags    = new List<string>();
                var warnings = new List<string>();

                // ── Mismatch: completed renewal has no invoice and no manual reference
                if (r.Status == CargoDryRenewalPreparationStatus.Completed
                    && !r.InvoiceId.HasValue
                    && string.IsNullOrWhiteSpace(r.ManualPaymentReference))
                    flags.Add("CompletedWithoutInvoiceOrManualReference");

                // ── Mismatch: invoice was prepared but renewal not completed (stale)
                if (r.InvoiceId.HasValue
                    && r.Status is not CargoDryRenewalPreparationStatus.Completed
                                and not CargoDryRenewalPreparationStatus.Cancelled)
                    flags.Add("InvoicePreparedButNotCompleted");

                // ── Mismatch: notification dispatched but renewal still in Prepared status
                if (r.NotificationStatus == CargoDryRenewalNotificationStatus.Dispatched
                    && r.Status == CargoDryRenewalPreparationStatus.Prepared)
                    flags.Add("NotificationDispatchedButNotCompleted");

                // ── Mismatch: renewal price is zero
                if (r.RenewalPrice == 0m)
                    flags.Add("MissingRenewalPrice");

                // ── Mismatch: currency code is missing
                if (string.IsNullOrWhiteSpace(r.CurrencyCode))
                    flags.Add("MissingCurrency");

                // ── Warning: notification failed
                if (r.NotificationStatus == CargoDryRenewalNotificationStatus.Failed)
                    warnings.Add("NotificationFailed");

                // ── Warning: completed but NewExpiresAtUtc is null
                if (r.Status == CargoDryRenewalPreparationStatus.Completed
                    && !r.NewExpiresAtUtc.HasValue)
                    warnings.Add("CompletedWithoutNewExpiryDate");

                return new CargoDryRenewalReconciliationRowDto
                {
                    RenewalPreparationId   = r.Id,
                    RenewalCode            = r.RenewalCode,
                    KitId                  = r.KitId,
                    KitCode                = r.KitCode,
                    OwnerUserId            = r.OwnerUserId,
                    VesselId               = r.VesselId,
                    ProductCode            = r.ProductCode,
                    Status                 = r.Status.ToString(),
                    RenewalPrice           = r.RenewalPrice,
                    CurrencyCode           = r.CurrencyCode ?? string.Empty,
                    InvoiceId              = r.InvoiceId,
                    ManualPaymentReference = r.ManualPaymentReference,
                    NotificationStatus     = r.NotificationStatus.ToString(),
                    CompletedAtUtc         = r.CompletedAtUtc,
                    NewExpiresAtUtc        = r.NewExpiresAtUtc,
                    PreparedAtUtc          = r.PreparedAtUtc,
                    MismatchFlags          = flags,
                    Warnings               = warnings,
                };
            })
            .ToList();

        // Apply hasMismatches filter in-memory after computation
        if (request.HasMismatches == true)
            rows = rows.Where(r => r.MismatchFlags.Count > 0).ToList();
        else if (request.HasMismatches == false)
            rows = rows.Where(r => r.MismatchFlags.Count == 0).ToList();

        var mismatchCount  = rows.Count(r => r.MismatchFlags.Count > 0);
        var effectiveTotal = rows.Count;
        var skip           = (request.Page - 1) * request.PageSize;
        var pagedItems     = rows.Skip(skip).Take(request.PageSize).ToList();

        return new GetCargoDryRenewalReconciliationReportResponse
        {
            Report = new CargoDryRenewalReconciliationReportDto
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
