using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>
/// Response DTO returned after a successful batch-to-provider allocation.
/// Carries all identifiers needed by the admin to confirm and link records.
/// Phase 2 — CargoDry provider inventory (July 2026).
/// </summary>
public sealed class AllocateBatchToProviderResultDto
{
    public string                  BatchCode                   { get; init; } = default!;
    public long                    ProviderProfileId           { get; init; }
    public string                  ProductCode                 { get; init; } = default!;
    public int                     AllocatedCount              { get; init; }
    public CargoDryCommercialModel CommercialModel             { get; init; }
    public string                  CommercialModelName         { get; init; } = default!;
    public SalesChannel            SalesChannel                { get; init; }
    public string                  SalesChannelName            { get; init; } = default!;
    public long?                   ConsignmentAgreementId      { get; init; }
    public long?                   WarehouseId                 { get; init; }
    public long                    InventoryId                 { get; init; }
    public long                    MovementId                  { get; init; }
    public int                     RemainingAgreementKitCount  { get; init; }
    public bool                    IsIdempotentResult          { get; init; }
}

/// <summary>
/// Preview response for GetBatchAllocationPreviewQuery.
/// Allows the admin modal to validate before executing allocation.
/// </summary>
public sealed class BatchAllocationPreviewDto
{
    public string                  BatchCode                   { get; init; } = default!;
    public string                  ProductCode                 { get; init; } = default!;
    public int                     AvailableKitCount           { get; init; }
    public long                    ProviderProfileId           { get; init; }
    public CargoDryCommercialModel CommercialModel             { get; init; }
    public string                  CommercialModelName         { get; init; } = default!;
    public long?                   ResolvedAgreementId         { get; init; }
    public string?                 AgreementStatus             { get; init; }
    public int                     AgreementRemainingKitCount  { get; init; }
    public bool                    CanAllocate                 { get; init; }
    public string?                 BlockingReason              { get; init; }
}
