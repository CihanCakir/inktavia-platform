using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;

// ── Provider Inventory BFF DTOs ───────────────────────────────────────────────
// Pass-through from CargoDry module. Phase 2 — Provider Inventory & Batch Allocation.

[DocumentationInfo("CargoDry provider inventory BFF DTO",
    "Full inventory row DTO passed through from the CargoDry admin inventory endpoint. " +
    "Phase 2 — CargoDry provider inventory (July 2026).")]
public sealed class CargoDryProviderInventoryBffDto
{
    public long    Id                { get; init; }
    public long    ProviderProfileId { get; init; }
    public string  ProductCode       { get; init; } = default!;
    public string? BatchCode         { get; init; }

    public CargoDryCommercialModel CommercialModel     { get; init; }
    public string                  CommercialModelName { get; init; } = default!;
    public SalesChannel            SalesChannel        { get; init; }
    public string                  SalesChannelName    { get; init; } = default!;
    public StockLocationType       StockLocationType   { get; init; }
    public string                  StockLocationName   { get; init; } = default!;

    public int TotalAllocated  { get; init; }
    public int TotalActivated  { get; init; }
    public int TotalRevoked    { get; init; }
    public int TotalReturned   { get; init; }
    public int TotalAdjusted   { get; init; }
    public int AvailableStock  { get; init; }

    public DateTime? LastMovementAtUtc { get; init; }
    public DateTime  CreatedAtUtc      { get; init; }
    public DateTime? UpdatedAtUtc      { get; init; }
}

[DocumentationInfo("CargoDry provider inventory list item BFF DTO",
    "Lightweight list item for the paged inventory list endpoint.")]
public sealed class CargoDryProviderInventoryListItemBffDto
{
    public long    Id                { get; init; }
    public long    ProviderProfileId { get; init; }
    public string  ProductCode       { get; init; } = default!;
    public string? BatchCode         { get; init; }

    public CargoDryCommercialModel CommercialModel     { get; init; }
    public string                  CommercialModelName { get; init; } = default!;
    public SalesChannel            SalesChannel        { get; init; }
    public string                  SalesChannelName    { get; init; } = default!;

    public int TotalAllocated { get; init; }
    public int TotalActivated { get; init; }
    public int AvailableStock { get; init; }

    public DateTime? LastMovementAtUtc { get; init; }
    public DateTime  CreatedAtUtc      { get; init; }
}

[DocumentationInfo("CargoDry provider inventory paged result BFF DTO",
    "Paged result wrapper for inventory list queries.")]
public sealed class CargoDryProviderInventoryPagedBffDto
{
    public List<CargoDryProviderInventoryListItemBffDto> Items    { get; init; } = [];
    public int                                           Total    { get; init; }
    public int                                           Page     { get; init; }
    public int                                           PageSize { get; init; }
}

[DocumentationInfo("CargoDry provider inventory detail BFF DTO",
    "Aggregate detail view for a single provider across all inventory rows.")]
public sealed class CargoDryProviderInventoryDetailBffDto
{
    public long ProviderProfileId { get; init; }

    public List<CargoDryProviderInventoryBffDto> InventoryRows { get; init; } = [];

    public int TotalAllocated { get; init; }
    public int TotalActivated { get; init; }
    public int TotalRevoked   { get; init; }
    public int TotalReturned  { get; init; }
    public int TotalAdjusted  { get; init; }
    public int TotalAvailable { get; init; }

    public DateTime? LastMovementAtUtc { get; init; }
}

// ── Inventory Movement BFF DTOs ───────────────────────────────────────────────

[DocumentationInfo("CargoDry inventory movement BFF DTO",
    "Single inventory movement ledger row passed through from the module.")]
public sealed class CargoDryInventoryMovementBffDto
{
    public long   Id                { get; init; }
    public long   ProviderProfileId { get; init; }
    public string ProductCode       { get; init; } = default!;
    public string? BatchCode        { get; init; }
    public long?   KitId            { get; init; }

    public InventoryMovementType MovementType     { get; init; }
    public string                MovementTypeName { get; init; } = default!;
    public int                   Quantity         { get; init; }
    public int?                  BalanceAfter     { get; init; }

    public CargoDryCommercialModel? CommercialModel     { get; init; }
    public string?                  CommercialModelName { get; init; }
    public SalesChannel?            SalesChannel        { get; init; }
    public string?                  SalesChannelName    { get; init; }

    public string? ReferenceType    { get; init; }
    public long?   ReferenceId      { get; init; }
    public string? Note             { get; init; }

    public DateTime CreatedAtUtc    { get; init; }
    public long?    CreatedByUserId { get; init; }
}

[DocumentationInfo("CargoDry inventory movement paged result BFF DTO",
    "Paged result wrapper for movement ledger queries.")]
public sealed class CargoDryInventoryMovementPagedBffDto
{
    public List<CargoDryInventoryMovementBffDto> Items    { get; init; } = [];
    public int                                   Total    { get; init; }
    public int                                   Page     { get; init; }
    public int                                   PageSize { get; init; }
}

// ── Allocation Result & Preview BFF DTOs ─────────────────────────────────────

[DocumentationInfo("Allocate batch to provider BFF result DTO",
    "Response after a successful batch→provider allocation.")]
public sealed class AllocateBatchToProviderBffResultDto
{
    public string                  BatchCode                  { get; init; } = default!;
    public long                    ProviderProfileId          { get; init; }
    public string                  ProductCode                { get; init; } = default!;
    public int                     AllocatedCount             { get; init; }
    public CargoDryCommercialModel CommercialModel            { get; init; }
    public string                  CommercialModelName        { get; init; } = default!;
    public SalesChannel            SalesChannel               { get; init; }
    public string                  SalesChannelName           { get; init; } = default!;
    public long?                   ConsignmentAgreementId     { get; init; }
    public long?                   WarehouseId                { get; init; }
    public long                    InventoryId                { get; init; }
    public long                    MovementId                 { get; init; }
    public int                     RemainingAgreementKitCount { get; init; }
    public bool                    IsIdempotentResult         { get; init; }
}

[DocumentationInfo("Batch allocation preview BFF DTO",
    "Pre-flight check result for a batch→provider allocation.")]
public sealed class BatchAllocationPreviewBffDto
{
    public string                  BatchCode                  { get; init; } = default!;
    public string                  ProductCode                { get; init; } = default!;
    public int                     AvailableKitCount          { get; init; }
    public long                    ProviderProfileId          { get; init; }
    public CargoDryCommercialModel CommercialModel            { get; init; }
    public string                  CommercialModelName        { get; init; } = default!;
    public long?                   ResolvedAgreementId        { get; init; }
    public string?                 AgreementStatus            { get; init; }
    public int                     AgreementRemainingKitCount { get; init; }
    public bool                    CanAllocate                { get; init; }
    public string?                 BlockingReason             { get; init; }
}
