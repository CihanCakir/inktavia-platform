using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Services;

/// <summary>
/// Resolves the commercial path for a newly activated CargoDry kit.
///
/// Called by ActivateKitCommandHandler after kit.Activate() + SaveChangesAsync().
/// All entity mutations are staged; the handler's final SaveChangesAsync flushes them.
///
/// Scope matrix:
/// ┌─────────────────────────┬─────────────────────────────────────────────────────────────┐
/// │ SalesChannel            │ Action                                                      │
/// ├─────────────────────────┼─────────────────────────────────────────────────────────────┤
/// │ ConsignmentSellThrough  │ SalesAttribution(SettlementPending) + SellThroughSettlement │
/// │ ProviderAttributedSale  │ SalesAttribution(Attributed) + inventory decrement          │
/// │ DirectSale              │ SalesAttribution(Attributed), no provider                   │
/// │ null / unknown          │ SalesAttribution(CommercialReviewRequired)                  │
/// └─────────────────────────┴─────────────────────────────────────────────────────────────┘
///
/// Phase 3 exclusions: no PaymentTransaction, no Invoice, no ProviderPayout.
/// </summary>
[DocumentationInfo("CargoDry Commercial Activation Service",
    "Resolves and persists the commercial attribution path after a kit is activated. " +
    "Phase 3 (July 2026): Sales Attribution & Sell-Through Settlement Foundation.")]
public sealed class CargoDryCommercialActivationService : ICargoDryCommercialActivationService
{
    private readonly ICargoDryKitRepository                  _kits;
    private readonly ICargoDryConsignmentAgreementRepository _agreements;
    private readonly ICargoDryProviderInventoryRepository    _inventories;
    private readonly ICargoDryInventoryMovementRepository    _movements;
    private readonly ICargoDrySalesAttributionRepository     _attributions;
    private readonly ICargoDrySellThroughSettlementRepository _settlements;
    private readonly ICargoDryProviderMilestoneEvaluator _milestones;
    private readonly ILogger<CargoDryCommercialActivationService> _logger;

    public CargoDryCommercialActivationService(
        ICargoDryKitRepository                   kits,
        ICargoDryConsignmentAgreementRepository  agreements,
        ICargoDryProviderInventoryRepository     inventories,
        ICargoDryInventoryMovementRepository     movements,
        ICargoDrySalesAttributionRepository      attributions,
        ICargoDrySellThroughSettlementRepository settlements,
        ICargoDryProviderMilestoneEvaluator      milestones,
        ILogger<CargoDryCommercialActivationService> logger)
    {
        _kits        = kits;
        _agreements  = agreements;
        _inventories = inventories;
        _movements   = movements;
        _attributions = attributions;
        _settlements  = settlements;
        _milestones   = milestones;
        _logger       = logger;
    }

    public async Task ResolveAsync(long kitId, long activatedByUserId, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load kit ────────────────────────────────────────────────────────────
        var kit = await _kits.GetByIdAsync(kitId, ct)
                  ?? throw new InvalidOperationException(
                      $"CargoDryCommercialActivationService: Kit {kitId} not found.");

        // ── Idempotency guard ────────────────────────────────────────────────────
        var existingAttribution = await _attributions.GetByKitIdAsync(kitId, ct);
        if (existingAttribution is not null)
            return; // already attributed — safe to re-enter

        // ── Resolve path ─────────────────────────────────────────────────────────
        if (kit.SalesChannel is null)
        {
            // No commercial path known — flag for manual review
            await CreateAttributionAsync(
                kit,
                CargoDrySalesAttributionStatus.CommercialReviewRequired,
                nowUtc,
                activatedByUserId,
                inventoryId: null,
                consignmentAgreementId: null,
                ct);
            return;
        }

        switch (kit.SalesChannel.Value)
        {
            case SalesChannel.ConsignmentSellThrough:
                await HandleConsignmentSellThroughAsync(kit, activatedByUserId, nowUtc, ct);
                break;

            case SalesChannel.ProviderAttributedSale:
                await HandleProviderAttributedSaleAsync(kit, activatedByUserId, nowUtc, ct);
                break;

            case SalesChannel.DirectSale:
            default:
                await HandleDirectSaleAsync(kit, activatedByUserId, nowUtc, ct);
                break;
        }
    }

    // ── ConsignmentSellThrough ───────────────────────────────────────────────────

    private async Task HandleConsignmentSellThroughAsync(
        CargoDryKitEntity kit,
        long              activatedByUserId,
        DateTime          nowUtc,
        CancellationToken ct)
    {
        // Resolve active consignment agreement
        CargoDryConsignmentAgreementEntity? agreement = null;
        if (kit.ConsignmentAgreementId.HasValue)
        {
            agreement = await _agreements.GetByIdAsync(kit.ConsignmentAgreementId.Value, ct);
        }
        else if (kit.ProviderProfileId.HasValue)
        {
            agreement = await _agreements.GetActiveForProviderProductAsync(
                kit.ProviderProfileId.Value, kit.ProductCode, nowUtc, ct);
        }

        if (agreement is null)
        {
            // No active agreement — flag for review
            await CreateAttributionAsync(
                kit,
                CargoDrySalesAttributionStatus.CommercialReviewRequired,
                nowUtc,
                activatedByUserId,
                inventoryId: null,
                consignmentAgreementId: null,
                ct);
            return;
        }

        // ── Phase 3.2: approved grouping = Provider + Currency + Product + Month ──
        // Monthly period bucket for this activation date (UTC).
        var periodStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEndUtc   = periodStartUtc.AddMonths(1);
        if (string.IsNullOrWhiteSpace(agreement.CurrencyCode))
            throw new AizenBusinessException(
                $"Consignment agreement {agreement.Id} has no CurrencyCode configured. " +
                "Financial resolution blocked. Set CurrencyCode on the agreement " +
                "(e.g. TRY for domestic operations) before activating kits under it.");
        var currencyCode = agreement.CurrencyCode!;

        // Find or create the open monthly settlement using the approved grouping key.
        var settlement = await _settlements.GetOpenForProviderCurrencyProductPeriodAsync(
            providerProfileId: agreement.ProviderProfileId,
            currencyCode:      currencyCode,
            productCode:       kit.ProductCode,
            periodStartUtc:    periodStartUtc,
            periodEndUtc:      periodEndUtc,
            ct);

        if (settlement is null)
        {
            var settlementCode = CargoDrySettlementCode.Generate(
                agreement.ProviderProfileId, currencyCode, kit.ProductCode, nowUtc);
            settlement = CargoDrySellThroughSettlementEntity.Create(
                settlementCode:         settlementCode,
                consignmentAgreementId: agreement.Id,
                providerProfileId:      agreement.ProviderProfileId,
                productCode:            kit.ProductCode,
                batchCode:              kit.BatchCode,
                currencyCode:           currencyCode,
                periodStartUtc:         periodStartUtc,
                periodEndUtc:           periodEndUtc,
                nowUtc:                 nowUtc);
            await _settlements.AddAsync(settlement, ct);
        }

        // Phase 3: record the consignment rate from the agreement.
        // SalePrice and CommissionAmount are null until payment data is available (Phase 4+).
        var salePrice        = (decimal?)null;
        var commissionRate   = agreement.ConsignmentRate;
        var commissionAmount = (decimal?)null;

        // Update settlement totals (Phase 3: use 0 placeholders — real amounts resolved in Phase 4)
        settlement.AddAttribution(salePrice ?? 0m, commissionAmount ?? 0m, nowUtc);

        // Materialise the settlement Id BEFORE creating the attribution so we can set the SellThroughSettlementId FK
        // inline (link-at-activation). A brand-new settlement is Id=0 until flushed; an existing one already has an Id.
        // Safe within the ActivateKit command transaction — this flush stays uncommitted until the handler commits.
        if (settlement.Id == 0)
            await _settlements.SaveChangesAsync(ct);

        // Resolve provider inventory row
        long? inventoryId = null;
        if (kit.ProviderProfileId.HasValue && kit.BatchCode is not null)
        {
            var inventory = await _inventories.GetByProviderProductBatchAsync(
                kit.ProviderProfileId.Value, kit.ProductCode, kit.BatchCode, ct);
            if (inventory is not null)
            {
                inventory.IncrementActivated(1, nowUtc);
                inventoryId = inventory.Id;

                await _movements.AddAsync(CargoDryInventoryMovementEntity.Create(
                    providerProfileId: kit.ProviderProfileId.Value,
                    productCode:       kit.ProductCode,
                    movementType:      InventoryMovementType.KitActivated,
                    quantity:          -1,
                    nowUtc:            nowUtc,
                    batchCode:         kit.BatchCode,
                    kitId:             kit.Id,
                    balanceAfter:      inventory.AvailableStock,
                    commercialModel:   kit.CommercialModel,
                    salesChannel:      kit.SalesChannel,
                    referenceType:     "Kit",
                    referenceId:       kit.Id,
                    createdByUserId:   activatedByUserId), ct);
            }
        }

        // Create attribution in SettlementPending status and link to the settlement.
        var attribution = await CreateAttributionAsync(
            kit,
            CargoDrySalesAttributionStatus.SettlementPending,
            nowUtc,
            activatedByUserId,
            inventoryId:            inventoryId,
            consignmentAgreementId: agreement.Id,
            ct,
            salePrice:       salePrice,
            commissionRate:  commissionRate,
            commissionAmount: commissionAmount,
            currencyCode:    currencyCode);

        // Link-at-activation: stamp the SellThroughSettlementId FK now that the settlement has a real Id. The attribution
        // was created directly in SettlementPending, so LinkToSettlement only sets the FK (re-asserting SettlementPending
        // is an idempotent no-op — see LinkToSettlement). Without this the FK was NULL forever and settlement resolution
        // could never find the attribution (the dead-link bug). The handler's final SaveChanges persists the FK.
        attribution.LinkToSettlement(settlement.Id, nowUtc);

        // ── Milestone evaluation (non-blocking) ────────────────────────────────
        if (kit.ProviderProfileId.HasValue)
        {
            try { await _milestones.EvaluateAfterSaleAsync(kit.ProviderProfileId.Value, DateTimeOffset.UtcNow, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Milestone eval failed for provider {Pid}", kit.ProviderProfileId); }
        }
    }

    // ── ProviderAttributedSale ───────────────────────────────────────────────────

    private async Task HandleProviderAttributedSaleAsync(
        CargoDryKitEntity kit,
        long              activatedByUserId,
        DateTime          nowUtc,
        CancellationToken ct)
    {
        long? inventoryId = null;

        if (kit.ProviderProfileId.HasValue && kit.BatchCode is not null)
        {
            var inventory = await _inventories.GetByProviderProductBatchAsync(
                kit.ProviderProfileId.Value, kit.ProductCode, kit.BatchCode, ct);
            if (inventory is not null)
            {
                inventory.IncrementActivated(1, nowUtc);
                inventoryId = inventory.Id;

                await _movements.AddAsync(CargoDryInventoryMovementEntity.Create(
                    providerProfileId: kit.ProviderProfileId.Value,
                    productCode:       kit.ProductCode,
                    movementType:      InventoryMovementType.KitActivated,
                    quantity:          -1,
                    nowUtc:            nowUtc,
                    batchCode:         kit.BatchCode,
                    kitId:             kit.Id,
                    balanceAfter:      inventory.AvailableStock,
                    commercialModel:   kit.CommercialModel,
                    salesChannel:      kit.SalesChannel,
                    referenceType:     "Kit",
                    referenceId:       kit.Id,
                    createdByUserId:   activatedByUserId), ct);
            }
        }

        await CreateAttributionAsync(
            kit,
            CargoDrySalesAttributionStatus.Attributed,
            nowUtc,
            activatedByUserId,
            inventoryId:            inventoryId,
            consignmentAgreementId: null,
            ct);

        // ── Milestone evaluation (non-blocking) ────────────────────────────────
        if (kit.ProviderProfileId.HasValue)
        {
            try { await _milestones.EvaluateAfterSaleAsync(kit.ProviderProfileId.Value, DateTimeOffset.UtcNow, ct); }
            catch (Exception ex) { _logger.LogError(ex, "Milestone eval failed for provider {Pid}", kit.ProviderProfileId); }
        }
    }

    // ── DirectSale ──────────────────────────────────────────────────────────────

    private Task HandleDirectSaleAsync(
        CargoDryKitEntity kit,
        long              activatedByUserId,
        DateTime          nowUtc,
        CancellationToken ct)
        => CreateAttributionAsync(
            kit,
            CargoDrySalesAttributionStatus.Attributed,
            nowUtc,
            activatedByUserId,
            inventoryId:            null,
            consignmentAgreementId: null,
            ct);

    // ── Helper ───────────────────────────────────────────────────────────────────

    private async Task<CargoDrySalesAttributionEntity> CreateAttributionAsync(
        CargoDryKitEntity               kit,
        CargoDrySalesAttributionStatus  status,
        DateTime                        nowUtc,
        long                            activatedByUserId,
        long?                           inventoryId,
        long?                           consignmentAgreementId,
        CancellationToken               ct,
        decimal?                        salePrice        = null,
        decimal?                        commissionRate   = null,
        decimal?                        commissionAmount = null,
        string?                         currencyCode     = null)
    {
        var entity = CargoDrySalesAttributionEntity.Create(
            kitId:                  kit.Id,
            serialNumber:           kit.SerialNumber,
            kitCode:                kit.KitCode,
            productCode:            kit.ProductCode,
            batchCode:              kit.BatchCode,
            salesChannel:           kit.SalesChannel ?? SalesChannel.DirectSale,
            commercialModel:        kit.CommercialModel ?? CargoDryCommercialModel.PrincipalSale,
            initialStatus:          status,
            nowUtc:                 nowUtc,
            providerProfileId:      kit.ProviderProfileId,
            consignmentAgreementId: consignmentAgreementId,
            inventoryId:            inventoryId,
            salePrice:              salePrice,
            commissionRate:         commissionRate,
            commissionAmount:       commissionAmount,
            currencyCode:           currencyCode,
            attributedByUserId:     status == CargoDrySalesAttributionStatus.Attributed
                                        ? activatedByUserId
                                        : null);

        // ── CE-6a-(b): snapshot provider tier at sale time ────────────────────
        if (kit.ProviderProfileId is { } pid)
        {
            var windowStart = new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero)
                .AddMonths(-11); // rolling 12mo including current month
            var endOfTime = DateTimeOffset.UtcNow.AddDays(1);
            var cumulative = await _attributions.SumProviderCommissionAsync(pid, windowStart, endOfTime, ct);
            var tier = CargoDryProviderTierConfig.Resolve(cumulative);
            entity.ApplyTierSnapshot(tier.Code, tier.BonusRate);
        }
        else
        {
            entity.ApplyTierSnapshot(null, 0m);
        }

        await _attributions.AddAsync(entity, ct);
        return entity;
    }
}
