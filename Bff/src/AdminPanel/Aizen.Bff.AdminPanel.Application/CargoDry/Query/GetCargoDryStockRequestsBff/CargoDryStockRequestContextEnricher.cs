using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryStockRequestsBff;

/// <summary>
/// Builds per-provider requester context (name + CargoDry standing + recent requests) for the admin stock-request
/// surfaces. Resolves each distinct provider once; per-provider CargoDry failures degrade to defaults (row still shows).
/// </summary>
public interface ICargoDryStockRequestContextEnricher
{
    Task<IReadOnlyDictionary<long, CargoDryStockRequestRequesterContextBffDto>> BuildAsync(
        IEnumerable<long> providerProfileIds, int recentRequestCount, CancellationToken ct);
}

public sealed class CargoDryStockRequestContextEnricher : ICargoDryStockRequestContextEnricher
{
    private const int ConsignmentAgreementStatusActive = 2; // ConsignmentAgreementStatus.Active

    private readonly IIdentityRemoteCall _identity;
    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly ILogger<CargoDryStockRequestContextEnricher> _logger;

    public CargoDryStockRequestContextEnricher(
        IIdentityRemoteCall identity, ICargoDryRemoteCall cargoDry,
        ILogger<CargoDryStockRequestContextEnricher> logger)
    {
        _identity = identity;
        _cargoDry = cargoDry;
        _logger   = logger;
    }

    public async Task<IReadOnlyDictionary<long, CargoDryStockRequestRequesterContextBffDto>> BuildAsync(
        IEnumerable<long> providerProfileIds, int recentRequestCount, CancellationToken ct)
    {
        var ids = providerProfileIds.Where(id => id > 0).Distinct().ToArray();
        var result = new Dictionary<long, CargoDryStockRequestRequesterContextBffDto>();
        if (ids.Length == 0) return result;

        // Names — one bulk Identity call (best-effort).
        var namesByProfileId = new Dictionary<long, string>();
        try
        {
            var profiles = await _identity.GetUserProfilesByProfileIds(ids);
            if (profiles?.Header?.IsSuccess == true && profiles.Body is not null)
                foreach (var p in profiles.Body)
                {
                    var name = $"{p.FirstName} {p.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(name)) namesByProfileId[p.Id] = name;
                }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Identity name enrichment failed for stock-request requester context.");
        }

        foreach (var pid in ids)
        {
            var ctx = new CargoDryStockRequestRequesterContextBffDto
            {
                ProviderProfileId = pid,
                ProviderName      = namesByProfileId.TryGetValue(pid, out var n) ? n : null,
            };

            try
            {
                var agreements = await _cargoDry.GetConsignmentAgreementsPagedAsync(
                    pid, null, ConsignmentAgreementStatusActive, null, null, null, 1, 1, ct);
                ctx.ActiveAgreementCount = agreements?.Total ?? 0;

                var batches = await _cargoDry.GetBatchesAsync(1, 1, pid, "allocated", ct);
                ctx.AllocatedBatchCount = batches?.Total ?? 0;

                var past = await _cargoDry.GetStockRequestsAsync(null, pid, 1, recentRequestCount, ct);
                ctx.PastRequestCount = past?.Total ?? 0;
                ctx.RecentRequests   = past?.Items ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CargoDry requester-context enrichment failed for provider {ProfileId}.", pid);
            }

            result[pid] = ctx;
        }

        return result;
    }
}
