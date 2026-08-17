using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryUsageReport;

[DocumentationInfo("Get CargoDry usage report BFF query handler", "Calls the CargoDry module's admin report endpoint and maps field-for-field to BFF DTO. No business logic in BFF.")]
public sealed class GetCargoDryUsageReportBffQueryHandler
    : AizenQueryHandler<GetCargoDryUsageReportBffQuery, GetCargoDryUsageReportBffResponse>
{
    private readonly ICargoDryRemoteCall _remote;

    public GetCargoDryUsageReportBffQueryHandler(ICargoDryRemoteCall remote)
        => _remote = remote;

    public override async Task<GetCargoDryUsageReportBffResponse> Handle(
        GetCargoDryUsageReportBffQuery request, CancellationToken ct)
    {
        var raw = await _remote.GetUsageReportAsync(request.DateFrom, request.DateTo, ct);

        var dto = new CargoDryKitUsageReportBffDto
        {
            TotalKits             = raw.TotalKits,
            ActivatedKits         = raw.ActivatedKits,
            ExpiredKits           = raw.ExpiredKits,
            RevokedKits           = raw.RevokedKits,
            RenewedKits           = raw.RenewedKits,
            AverageEfficiencyPct  = raw.AverageEfficiencyPct,
            RenewalRatePct        = raw.RenewalRatePct,
            AvgActiveDaysAtExpiry = raw.AvgActiveDaysAtExpiry,
            GeneratedAt           = raw.GeneratedAt,
            ByProduct = raw.ByProduct.Select(p => new CargoDryProductUsageRowBffDto
            {
                ProductCode      = p.ProductCode,
                ProductName      = p.ProductName,
                TotalKits        = p.TotalKits,
                ActivatedKits    = p.ActivatedKits,
                ExpiredKits      = p.ExpiredKits,
                RenewedKits      = p.RenewedKits,
                AvgEfficiencyPct = p.AvgEfficiencyPct,
            }).ToList(),
            ByBatch = raw.ByBatch.Select(b => new CargoDryBatchUsageRowBffDto
            {
                BatchCode     = b.BatchCode,
                ProductCode   = b.ProductCode,
                TotalKits     = b.TotalKits,
                ActivatedKits = b.ActivatedKits,
                ExpiredKits   = b.ExpiredKits,
                CreatedAt     = b.CreatedAt,
            }).ToList(),
            DailyActivations = raw.DailyActivations.Select(d => new CargoDryDailyActivationBffDto
            {
                Date        = d.Date,
                Activations = d.Activations,
                Renewals    = d.Renewals,
            }).ToList(),
        };

        return new GetCargoDryUsageReportBffResponse { Report = dto };
    }
}
