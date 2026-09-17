using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <inheritdoc cref="ICargoDrySettlementLinkingService"/>
[DocumentationInfo("CargoDry Settlement Linking Service (second-pass healer)",
    "Links orphaned ConsignmentSellThrough attributions to their period settlement (creating it if missing). " +
    "Idempotent; invoked by the monthly settlement automation. Does NOT recompute settlement totals — those are " +
    "authoritatively recalculated at resolution time (ResolveMonthlySellThroughSettlement).")]
public sealed class CargoDrySettlementLinkingService : ICargoDrySettlementLinkingService
{
    private readonly ICargoDrySalesAttributionRepository      _attributions;
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ILogger<CargoDrySettlementLinkingService> _logger;

    public CargoDrySettlementLinkingService(
        ICargoDrySalesAttributionRepository       attributions,
        ICargoDrySellThroughSettlementRepository  settlements,
        ILogger<CargoDrySettlementLinkingService> logger)
    {
        _attributions = attributions;
        _settlements  = settlements;
        _logger       = logger;
    }

    public async Task<int> LinkUnlinkedForPeriodAsync(int targetYearMonth, long triggeredByUserId, CancellationToken ct)
    {
        var year  = targetYearMonth / 100;
        var month = targetYearMonth % 100;
        var periodStartUtc = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEndUtc   = periodStartUtc.AddMonths(1);
        var nowUtc         = DateTime.UtcNow;

        var unlinked = await _attributions.GetUnlinkedConsignmentSellThroughForPeriodAsync(
            periodStartUtc, periodEndUtc, ct);
        if (unlinked.Count == 0)
            return 0;

        // Group by the settlement grouping key (Provider + Currency + Product + Agreement, within the period).
        var groups = unlinked.GroupBy(a => (
            a.ProviderProfileId,
            a.CurrencyCode,
            a.ProductCode,
            a.ConsignmentAgreementId));

        var linked = 0;

        foreach (var group in groups)
        {
            var (providerProfileId, currencyCode, productCode, agreementId) = group.Key;

            // A valid settlement needs a provider, a currency, and the source agreement. Skip (don't crash) any orphan
            // that can't form a key — surface it for manual review instead.
            if (providerProfileId is null || string.IsNullOrWhiteSpace(currencyCode) || agreementId is null)
            {
                _logger.LogWarning(
                    "Second-pass link: skipping {Count} orphan attribution(s) for product {Product} in period {Period} " +
                    "— missing provider/currency/agreement (provider={Provider}, currency={Currency}, agreement={Agreement}).",
                    group.Count(), productCode, targetYearMonth, providerProfileId, currencyCode, agreementId);
                continue;
            }

            var settlement = await _settlements.GetByProviderCurrencyProductPeriodAsync(
                providerProfileId.Value, currencyCode!, productCode, periodStartUtc, periodEndUtc, ct);

            if (settlement is null)
            {
                var code = CargoDrySettlementCode.Generate(
                    providerProfileId.Value, currencyCode!, productCode, periodStartUtc);
                settlement = CargoDrySellThroughSettlementEntity.Create(
                    settlementCode:         code,
                    consignmentAgreementId: agreementId.Value,
                    providerProfileId:      providerProfileId.Value,
                    productCode:            productCode,
                    batchCode:              null, // group may span batches
                    currencyCode:           currencyCode!,
                    periodStartUtc:         periodStartUtc,
                    periodEndUtc:           periodEndUtc,
                    nowUtc:                 nowUtc);
                await _settlements.AddAsync(settlement, ct);
                await _settlements.SaveChangesAsync(ct); // materialise Id before linking
            }

            foreach (var attribution in group)
            {
                try
                {
                    // Link only. Settlement totals are NOT touched here — ResolveMonthly recomputes them from the linked
                    // set, so bumping running totals now would double-count (activation may already have counted them).
                    attribution.LinkToSettlement(settlement.Id, nowUtc);
                    linked++;
                }
                catch (InvalidOperationException ex)
                {
                    // Already-linked (concurrent run) or terminal status — safe to skip; keeps the pass idempotent.
                    _logger.LogWarning(ex,
                        "Second-pass link: could not link attribution {AttributionId} to settlement {SettlementId}.",
                        attribution.Id, settlement.Id);
                }
            }
        }

        await _attributions.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Second-pass link for period {Period}: linked {Linked} of {Total} unlinked attribution(s).",
            targetYearMonth, linked, unlinked.Count);

        return linked;
    }
}
