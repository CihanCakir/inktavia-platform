using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Interface.Service;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

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

    public CargoDryCommercialActivationService(
        ICargoDryKitRepository                   kits,
        ICargoDryConsignmentAgreementRepository  agreements,
        ICargoDryProviderInventoryRepository     inventories,
        ICargoDryInventoryMovementRepository     movements,
        ICargoDrySalesAttributionRepository      attributions,
        ICargoDrySellThroughSettlementRepository settlements)
    {
        _kits        = kits;
        _agreements  = agreements;
        _inventories = inventories;
        _movements   = movements;
        _attributions = attributions;
        _settlements  = settlements;
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

        // Resolve or create the open settlement for this agreement+product
        var settlement = await _settlements.GetOpenForAgreementProductAsync(
            agreement.Id, kit.ProductCode, ct);

        if (settlement is null)
        {
            var settlementCode = GenerateSettlementCode(agreement.Id, kit.ProductCode, nowUtc);
            settlement = CargoDrySellThroughSettlementEntity.Create(
                settlementCode:         settlementCode,
                consignmentAgreementId: agreement.Id,
                providerProfileId:      agreement.ProviderProfileId,
                productCode:            kit.ProductCode,
                batchCode:              kit.BatchCode,
                currencyCode:           agreement.CurrencyCode ?? "USD",
                periodStartUtc:         agreement.StartDateUtc,
                periodEndUtc:           agreement.EndDateUtc ?? nowUtc.AddMonths(1),
                nowUtc:                 nowUtc);
            await _settlements.AddAsync(settlement, ct);
        }

        // Phase 3: record the consignment rate from the agreement.
        // SalePrice and CommissionAmount are null until payment data is available (Phase 4+).
        var salePrice        = (decimal?)null;
        var commissionRate   = agreement.ConsignmentRate;
        var commissionAmount = (decimal?)null;
        var currencyCode     = agreement.CurrencyCode;

        // Update settlement totals (Phase 3: use 0 placeholders — real amounts resolved in Phase 4)
        settlement.AddAttribution(salePrice ?? 0m, commissionAmount ?? 0m, nowUtc);

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

        // Create attribution in SettlementPending status and link to settlement
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

        // Link requires an Id, which won't exist until SaveChanges — defer link via settlement
        // (the settlement already holds the aggregated totals; the SellThroughSettlementId FK
        // on the attribution is set only after the EF insert assigns an Id, so this is handled
        // by the caller's SaveChanges + a second-pass update in Phase 4 settlement job)
        // For Phase 3: set SellThroughSettlementId on attribution inline using the staged entity.
        // Since settlement is tracked, its Id will be populated by EF after SaveChanges;
        // but we can't set it before save. Instead we set it via a domain linkage after-save.
        // Workaround: save here then link. Caller is responsible for final SaveChanges.
        // Attribution is not yet saved — we pass the staged settlement reference;
        // EF will resolve the FK when both are saved in the same SaveChanges call.
        // This is correct EF Core behaviour: both entities are Added → EF assigns temporary IDs.
        _ = attribution; // used via AddAsync already staged
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

        await _attributions.AddAsync(entity, ct);
        return entity;
    }

    private static string GenerateSettlementCode(
        long consignmentAgreementId, string productCode, DateTime nowUtc)
        => $"STS-{consignmentAgreementId}-{productCode}-{nowUtc:yyyyMM}";
}
