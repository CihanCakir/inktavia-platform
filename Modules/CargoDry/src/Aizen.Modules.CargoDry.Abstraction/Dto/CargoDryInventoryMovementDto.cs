using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Abstraction.Dto;

/// <summary>DTO for a single inventory movement ledger row.</summary>
public sealed class CargoDryInventoryMovementDto
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

    public CargoDryCommercialModel? CommercialModel    { get; init; }
    public string?                  CommercialModelName { get; init; }
    public SalesChannel?            SalesChannel       { get; init; }
    public string?                  SalesChannelName   { get; init; }

    public string? ReferenceType    { get; init; }
    public long?   ReferenceId      { get; init; }
    public string? Note             { get; init; }

    public DateTime CreatedAtUtc    { get; init; }
    public long?    CreatedByUserId { get; init; }
}

/// <summary>Paged result wrapper for inventory movement list queries.</summary>
public sealed class CargoDryInventoryMovementPagedResultDto
{
    public List<CargoDryInventoryMovementDto> Items    { get; init; } = [];
    public int                                Total    { get; init; }
    public int                                Page     { get; init; }
    public int                                PageSize { get; init; }
}
