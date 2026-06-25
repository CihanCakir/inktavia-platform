using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryUsageReport;

public sealed class GetCargoDryUsageReportQueryHandler
    : AizenQueryHandler<GetCargoDryUsageReportQuery, GetCargoDryUsageReportResponse>
{
    private readonly ICargoDryKitRepository     _kits;
    private readonly ICargoDryBatchRepository   _batches;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryUsageReportQueryHandler(
        ICargoDryKitRepository kits,
        ICargoDryBatchRepository batches,
        ICargoDryProductRepository products)
    {
        _kits     = kits;
        _batches  = batches;
        _products = products;
    }

    public override async Task<GetCargoDryUsageReportResponse> Handle(
        GetCargoDryUsageReportQuery request, CancellationToken ct)
    {
        var from = request.DateFrom ?? DateTimeOffset.UtcNow.AddMonths(-3);
        var to   = request.DateTo   ?? DateTimeOffset.UtcNow;

        var allKits   = await _kits.GetAllForReportAsync(from, to, ct);
        var batches   = await _batches.GetAllForReportAsync(from, to, ct);
        var allProd   = await _products.GetAllActiveAsync(ct);
        var prodMap   = allProd.ToDictionary(p => p.ProductCode);

        // ── Summary ───────────────────────────────────────────────────────────
        var activated = allKits.Where(k => k.Status != CargoDryKitStatus.Available).ToList();
        var expired   = allKits.Where(k => k.Status == CargoDryKitStatus.Expired).ToList();
        var revoked   = allKits.Where(k => k.Status == CargoDryKitStatus.Revoked).ToList();
        var renewed   = allKits.Where(k => k.RenewalCount > 0).ToList();
        var active    = allKits.Where(k => k.Status == CargoDryKitStatus.Activated).ToList();

        double avgEff = active.Count > 0 ? active.Average(k => k.EfficiencyPercent) : 0d;

        double avgActiveDaysAtExpiry = expired.Count > 0
            ? expired
                .Where(k => k.ActivatedAt.HasValue && k.ExpiresAt.HasValue)
                .DefaultIfEmpty()
                .Average(k => k is null ? 0d : (k.ExpiresAt!.Value - k.ActivatedAt!.Value).TotalDays)
            : 0d;

        // ── By Product ────────────────────────────────────────────────────────
        var byProduct = allKits
            .GroupBy(k => k.ProductCode)
            .Select(g => new CargoDryProductUsageRow
            {
                ProductCode      = g.Key,
                ProductName      = prodMap.TryGetValue(g.Key, out var p) ? p.Name : g.Key,
                TotalKits        = g.Count(),
                ActivatedKits    = g.Count(k => k.Status != CargoDryKitStatus.Available),
                ExpiredKits      = g.Count(k => k.Status == CargoDryKitStatus.Expired),
                RenewedKits      = g.Count(k => k.RenewalCount > 0),
                AvgEfficiencyPct = g.Where(k => k.Status == CargoDryKitStatus.Activated)
                    .Select(k => k.EfficiencyPercent)
                    .DefaultIfEmpty(0d)
                    .Average(),
            })
            .OrderByDescending(r => r.TotalKits)
            .ToList();

        // ── By Batch ──────────────────────────────────────────────────────────
        var byBatch = batches
            .Select(b => new CargoDryBatchUsageRow
            {
                BatchCode     = b.BatchCode,
                ProductCode   = b.ProductCode,
                TotalKits     = allKits.Count(k => k.BatchCode == b.BatchCode),
                ActivatedKits = allKits.Count(k => k.BatchCode == b.BatchCode && k.Status != CargoDryKitStatus.Available),
                ExpiredKits   = allKits.Count(k => k.BatchCode == b.BatchCode && k.Status == CargoDryKitStatus.Expired),
                CreatedAt     = b.CreateDate.HasValue ? new DateTimeOffset(b.CreateDate.Value, TimeSpan.Zero) : DateTimeOffset.MinValue,
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        // ── Daily Activations (last 30 days) ──────────────────────────────────
        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
        var dailyActivations = allKits
            .Where(k => k.ActivatedAt >= thirtyDaysAgo)
            .GroupBy(k => DateOnly.FromDateTime(k.ActivatedAt!.Value.UtcDateTime))
            .Select(g => new CargoDryDailyActivationPoint
            {
                Date        = g.Key,
                Activations = g.Count(),
                Renewals    = g.Count(k => k.RenewalCount > 0),
            })
            .OrderBy(p => p.Date)
            .ToList();

        var report = new CargoDryKitUsageReportDto
        {
            TotalKits             = allKits.Count,
            ActivatedKits         = activated.Count,
            ExpiredKits           = expired.Count,
            RevokedKits           = revoked.Count,
            RenewedKits           = renewed.Count,
            AverageEfficiencyPct  = Math.Round(avgEff, 1),
            RenewalRatePct        = activated.Count > 0
                ? Math.Round(renewed.Count / (double)activated.Count * 100d, 1)
                : 0d,
            AvgActiveDaysAtExpiry = Math.Round(avgActiveDaysAtExpiry, 1),
            ByProduct             = byProduct,
            ByBatch               = byBatch,
            DailyActivations      = dailyActivations,
            GeneratedAt           = DateTimeOffset.UtcNow,
            DateFrom              = request.DateFrom,
            DateTo                = request.DateTo,
        };

        return new GetCargoDryUsageReportResponse { Report = report };
    }
}
