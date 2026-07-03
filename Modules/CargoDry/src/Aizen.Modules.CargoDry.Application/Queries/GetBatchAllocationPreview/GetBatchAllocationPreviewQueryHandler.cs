using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetBatchAllocationPreview;

[DocumentationInfo("Get batch allocation preview query handler",
    "Validates a batch→provider allocation before executing it. Returns CanAllocate flag and " +
    "blocking reason when allocation would fail. Used by the admin modal to show a pre-flight summary. " +
    "Phase 2 — CargoDry provider inventory (July 2026).")]
public sealed class GetBatchAllocationPreviewQueryHandler
    : AizenQueryHandler<GetBatchAllocationPreviewQuery, BatchAllocationPreviewDto>
{
    private readonly ICargoDryBatchRepository               _batches;
    private readonly ICargoDryKitRepository                 _kits;
    private readonly ICargoDryConsignmentAgreementRepository _agreements;
    private readonly ICargoDryProviderInventoryRepository   _inventories;

    public GetBatchAllocationPreviewQueryHandler(
        ICargoDryBatchRepository               batches,
        ICargoDryKitRepository                 kits,
        ICargoDryConsignmentAgreementRepository agreements,
        ICargoDryProviderInventoryRepository   inventories)
    {
        _batches     = batches;
        _kits        = kits;
        _agreements  = agreements;
        _inventories = inventories;
    }

    public override async Task<BatchAllocationPreviewDto> Handle(
        GetBatchAllocationPreviewQuery request, CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;

        // ── Load batch ────────────────────────────────────────────────────────
        var batch = await _batches.GetByCodeAsync(request.BatchCode, ct);
        if (batch is null)
        {
            return new BatchAllocationPreviewDto
            {
                BatchCode           = request.BatchCode,
                ProductCode         = string.Empty,
                AvailableKitCount   = 0,
                ProviderProfileId   = request.ProviderProfileId,
                CommercialModel     = request.CommercialModel,
                CommercialModelName = request.CommercialModel.ToString(),
                CanAllocate         = false,
                BlockingReason      = $"Batch '{request.BatchCode}' was not found.",
            };
        }

        // ── Revoked batch check ───────────────────────────────────────────────
        if (batch.IsRevoked)
        {
            return new BatchAllocationPreviewDto
            {
                BatchCode           = batch.BatchCode,
                ProductCode         = batch.ProductCode,
                AvailableKitCount   = 0,
                ProviderProfileId   = request.ProviderProfileId,
                CommercialModel     = request.CommercialModel,
                CommercialModelName = request.CommercialModel.ToString(),
                CanAllocate         = false,
                BlockingReason      = $"Batch '{batch.BatchCode}' is revoked and cannot be allocated.",
            };
        }

        // ── Already allocated to a different provider ─────────────────────────
        if (batch.AssignedProviderProfileId.HasValue &&
            batch.AssignedProviderProfileId.Value != request.ProviderProfileId)
        {
            return new BatchAllocationPreviewDto
            {
                BatchCode           = batch.BatchCode,
                ProductCode         = batch.ProductCode,
                AvailableKitCount   = 0,
                ProviderProfileId   = request.ProviderProfileId,
                CommercialModel     = request.CommercialModel,
                CommercialModelName = request.CommercialModel.ToString(),
                CanAllocate         = false,
                BlockingReason      =
                    $"Batch '{batch.BatchCode}' is already allocated to a different provider " +
                    $"(id: {batch.AssignedProviderProfileId.Value}).",
            };
        }

        // ── Idempotency — same allocation already exists ──────────────────────
        var existingInventory = await _inventories.GetByProviderProductBatchAsync(
            request.ProviderProfileId, batch.ProductCode, batch.BatchCode, ct);

        if (existingInventory is not null)
        {
            var availableKits = await _kits.GetAvailableByBatchCodeAsync(batch.BatchCode, ct);
            return new BatchAllocationPreviewDto
            {
                BatchCode           = batch.BatchCode,
                ProductCode         = batch.ProductCode,
                AvailableKitCount   = availableKits.Count,
                ProviderProfileId   = request.ProviderProfileId,
                CommercialModel     = request.CommercialModel,
                CommercialModelName = request.CommercialModel.ToString(),
                CanAllocate         = false,
                BlockingReason      =
                    $"Batch '{batch.BatchCode}' is already allocated to provider {request.ProviderProfileId} " +
                    "with the same commercial model (idempotent — allocation already completed).",
            };
        }

        // ── Available kits ────────────────────────────────────────────────────
        var kits = await _kits.GetAvailableByBatchCodeAsync(batch.BatchCode, ct);
        if (kits.Count == 0)
        {
            return new BatchAllocationPreviewDto
            {
                BatchCode           = batch.BatchCode,
                ProductCode         = batch.ProductCode,
                AvailableKitCount   = 0,
                ProviderProfileId   = request.ProviderProfileId,
                CommercialModel     = request.CommercialModel,
                CommercialModelName = request.CommercialModel.ToString(),
                CanAllocate         = false,
                BlockingReason      =
                    $"Batch '{batch.BatchCode}' has no available (un-activated) kits to allocate.",
            };
        }

        // ── ConsignmentSellThrough — resolve agreement ────────────────────────
        if (request.SalesChannel == SalesChannel.ConsignmentSellThrough)
        {
            var agreement = await _agreements.GetActiveForProviderProductAsync(
                request.ProviderProfileId, batch.ProductCode, nowUtc, ct);

            if (agreement is null)
            {
                return new BatchAllocationPreviewDto
                {
                    BatchCode                  = batch.BatchCode,
                    ProductCode                = batch.ProductCode,
                    AvailableKitCount          = kits.Count,
                    ProviderProfileId          = request.ProviderProfileId,
                    CommercialModel            = request.CommercialModel,
                    CommercialModelName        = request.CommercialModel.ToString(),
                    ResolvedAgreementId        = null,
                    AgreementStatus            = null,
                    AgreementRemainingKitCount = 0,
                    CanAllocate                = false,
                    BlockingReason             =
                        $"No active consignment agreement found for provider {request.ProviderProfileId} " +
                        $"and product '{batch.ProductCode}'. Create and activate an agreement first.",
                };
            }

            var canAllocate = agreement.CanAllocate(kits.Count, nowUtc);
            return new BatchAllocationPreviewDto
            {
                BatchCode                  = batch.BatchCode,
                ProductCode                = batch.ProductCode,
                AvailableKitCount          = kits.Count,
                ProviderProfileId          = request.ProviderProfileId,
                CommercialModel            = request.CommercialModel,
                CommercialModelName        = request.CommercialModel.ToString(),
                ResolvedAgreementId        = agreement.Id,
                AgreementStatus            = agreement.Status.ToString(),
                AgreementRemainingKitCount = agreement.RemainingKitCount,
                CanAllocate                = canAllocate,
                BlockingReason             = canAllocate
                    ? null
                    : $"Consignment agreement cap exceeded: {kits.Count} kit(s) requested but " +
                      $"only {agreement.RemainingKitCount} slot(s) remain in agreement " +
                      $"'{agreement.AgreementCode}' (max {agreement.MaxKitCount}, " +
                      $"allocated {agreement.AllocatedKitCount}).",
            };
        }

        // ── DirectSale / ProviderResale — no agreement needed ─────────────────
        return new BatchAllocationPreviewDto
        {
            BatchCode                  = batch.BatchCode,
            ProductCode                = batch.ProductCode,
            AvailableKitCount          = kits.Count,
            ProviderProfileId          = request.ProviderProfileId,
            CommercialModel            = request.CommercialModel,
            CommercialModelName        = request.CommercialModel.ToString(),
            ResolvedAgreementId        = null,
            AgreementStatus            = null,
            AgreementRemainingKitCount = 0,
            CanAllocate                = true,
            BlockingReason             = null,
        };
    }
}
