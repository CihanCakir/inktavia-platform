using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOperationalAlerts;

/// <summary>
/// Derives operational alerts at query time from kit repository state.
/// No persisted alert table is used.
/// Alert types:
///   ExpiringSoon     — kits expiring within 30 days (Severity: Warning)
///   RenewalDue       — kits expiring within 7 days  (Severity: Critical)
///   Expired          — kits still Active/Activated but past expiry (Severity: Critical)
///   CommercialReviewRequired — kits with CommercialReviewRequired status (Severity: Warning)
///   Revoked          — kits revoked in the last 30 days (informational)  (Severity: Info)
/// </summary>
public sealed class GetCargoDryOperationalAlertsQueryHandler
    : AizenQueryHandler<GetCargoDryOperationalAlertsQuery, CargoDryOperationalAlertsResponse>
{
    private readonly ICargoDryKitRepository _kits;

    public GetCargoDryOperationalAlertsQueryHandler(ICargoDryKitRepository kits)
        => _kits = kits;

    public override async Task<CargoDryOperationalAlertsResponse> Handle(
        GetCargoDryOperationalAlertsQuery request, CancellationToken ct)
    {
        // ── Gather alert sources in parallel ─────────────────────────────────
        var expiringSoonTask = _kits.GetExpiringAsync(30, ct);
        var expiredTask      = _kits.GetExpiredUnmarkedAsync(ct);
        var commercialTask   = _kits.GetPagedAsync(
            status: CargoDryKitStatus.CommercialReviewRequired,
            search: null, vesselId: null, ownerUserId: null, batchCode: null,
            skip: 0, take: 500, ct: ct);
        var revokedTask = _kits.GetPagedAsync(
            status: CargoDryKitStatus.Revoked,
            search: null, vesselId: null, ownerUserId: null, batchCode: null,
            skip: 0, take: 200, ct: ct);

        await Task.WhenAll(expiringSoonTask, expiredTask, commercialTask, revokedTask);

        var expiringSoon = await expiringSoonTask;
        var expired      = await expiredTask;
        var (commercialKits, _) = await commercialTask;
        var (revokedKits, _)    = await revokedTask;

        var now    = DateTimeOffset.UtcNow;
        var alerts = new List<CargoDryOperationalAlertDto>();

        // ── ExpiringSoon / RenewalDue ─────────────────────────────────────────
        foreach (var kit in expiringSoon)
        {
            var daysLeft   = kit.ExpiresAt.HasValue
                ? (int)Math.Ceiling((kit.ExpiresAt.Value - now).TotalDays)
                : 0;
            var isCritical = daysLeft <= 7;

            alerts.Add(MapAlert(kit,
                alertType: isCritical ? "RenewalDue" : "ExpiringSoon",
                severity:  isCritical ? CargoDryAlertSeverity.Critical : CargoDryAlertSeverity.Warning,
                message:   isCritical
                    ? $"Kit {kit.KitCode} expires in {daysLeft} day(s) — renewal urgently required."
                    : $"Kit {kit.KitCode} expires in {daysLeft} day(s).",
                daysUntilExpiry: daysLeft));
        }

        // ── Expired (unmarked) ────────────────────────────────────────────────
        foreach (var kit in expired)
        {
            var daysOverdue = kit.ExpiresAt.HasValue
                ? (int)Math.Floor((now - kit.ExpiresAt.Value).TotalDays)
                : 0;
            alerts.Add(MapAlert(kit,
                alertType: "Expired",
                severity:  CargoDryAlertSeverity.Critical,
                message:   $"Kit {kit.KitCode} expired {daysOverdue} day(s) ago and has not been marked expired.",
                daysUntilExpiry: -daysOverdue));
        }

        // ── CommercialReviewRequired ──────────────────────────────────────────
        foreach (var kit in commercialKits)
        {
            alerts.Add(MapAlert(kit,
                alertType: "CommercialReviewRequired",
                severity:  CargoDryAlertSeverity.Warning,
                message:   $"Kit {kit.KitCode} is pending commercial attribution review.",
                daysUntilExpiry: null));
        }

        // ── Revoked (informational) ───────────────────────────────────────────
        foreach (var kit in revokedKits)
        {
            alerts.Add(MapAlert(kit,
                alertType: "Revoked",
                severity:  CargoDryAlertSeverity.Info,
                message:   $"Kit {kit.KitCode} has been revoked.",
                daysUntilExpiry: null));
        }

        // ── Pagination ────────────────────────────────────────────────────────
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var page     = Math.Max(request.Page, 1);

        var total = alerts.Count;
        var paged = alerts
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new CargoDryOperationalAlertsResponse
        {
            Items    = paged,
            Total    = total,
            Page     = page,
            PageSize = pageSize,
        };
    }

    private static CargoDryOperationalAlertDto MapAlert(
        CargoDryKitEntity      kit,
        string                 alertType,
        CargoDryAlertSeverity  severity,
        string                 message,
        int?                   daysUntilExpiry)
        => new()
        {
            KitId             = kit.Id,
            KitCode           = kit.KitCode,
            SerialNumber      = kit.SerialNumber,
            BatchCode         = kit.BatchCode,
            ProductCode       = kit.ProductCode,
            ProductName       = kit.ProductCode,           // enrichment done in BFF if needed
            OwnerUserId       = kit.OwnerUserId,
            OwnerDisplayName  = kit.OwnerUserId?.ToString(), // enrichment in BFF
            VesselId          = kit.VesselId,
            VesselName        = kit.VesselId?.ToString(),    // enrichment in BFF
            ProviderProfileId = kit.ProviderProfileId,
            AlertType         = alertType,
            Severity          = severity,
            Message           = message,
            DaysUntilExpiry   = daysUntilExpiry,
            ExpiresAt         = kit.ExpiresAt,
            KitStatus         = kit.Status.ToString(),
        };
}
