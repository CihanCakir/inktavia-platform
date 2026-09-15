using Aizen.Bff.AdminPanel.Application.Vessels.Dto; // CargoDryBatchListBffDto
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Dto;

/// <summary>
/// Requester context attached to each admin stock-request row: who the provider is and their CargoDry standing.
/// Assembled once per distinct provider on the page (bounded), degrading gracefully on per-provider read failures.
/// </summary>
public sealed class CargoDryStockRequestRequesterContextBffDto
{
    public long    ProviderProfileId    { get; set; }
    public string? ProviderName         { get; set; }
    public int     ActiveAgreementCount { get; set; }
    public int     AllocatedBatchCount  { get; set; }
    public int     PastRequestCount     { get; set; }
    /// <summary>The provider's most recent stock requests (last N), for at-a-glance history on the row.</summary>
    public List<CargoDryStockRequestDto> RecentRequests { get; set; } = new();
}

/// <summary>An admin stock-request list row: the request plus its requester context.</summary>
public sealed class CargoDryStockRequestAdminRowBffDto
{
    public CargoDryStockRequestDto                     Request   { get; set; } = default!;
    public CargoDryStockRequestRequesterContextBffDto? Requester { get; set; }
}

public sealed class CargoDryStockRequestAdminListBffResponse
{
    public List<CargoDryStockRequestAdminRowBffDto> Items    { get; set; } = new();
    public int Total    { get; set; }
    public int Page     { get; set; }
    public int PageSize { get; set; }
}

/// <summary>Full admin detail for one stock request: the request, requester context, that provider's request
/// history, the batches allocated to them, and their CargoDry inventory movements.</summary>
public sealed class CargoDryStockRequestAdminDetailBffResponse
{
    public CargoDryStockRequestDto                     Request            { get; set; } = default!;
    public CargoDryStockRequestRequesterContextBffDto? Requester          { get; set; }
    public List<CargoDryStockRequestDto>               RequestHistory     { get; set; } = new();
    public CargoDryBatchListBffDto?                    AllocatedBatches   { get; set; }
    public CargoDryInventoryMovementPagedBffDto?       InventoryMovements { get; set; }
}
