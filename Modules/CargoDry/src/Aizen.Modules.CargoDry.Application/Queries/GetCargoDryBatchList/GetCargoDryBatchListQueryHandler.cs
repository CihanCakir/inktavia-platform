using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryBatchList;

public sealed class GetCargoDryBatchListQueryHandler
    : AizenQueryHandler<GetCargoDryBatchListQuery, CargoDryBatchListDto>
{
    private readonly ICargoDryBatchRepository   _batches;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryBatchListQueryHandler(
        ICargoDryBatchRepository batches,
        ICargoDryProductRepository products)
    {
        _batches  = batches;
        _products = products;
    }

    public override async Task<CargoDryBatchListDto> Handle(
        GetCargoDryBatchListQuery request, CancellationToken ct)
    {
        var all   = await _batches.GetAllAsync(ct);
        var prods = (await _products.GetAllActiveAsync(ct))
            .ToDictionary(p => p.ProductCode);

        // ADDENDUM A3 — additive filters (in-memory, matching the existing paging style).
        IEnumerable<Domain.Entities.CargoDryBatchEntity> filtered = all;
        if (request.AssignedProviderProfileId is { } pid)
            filtered = filtered.Where(b => b.AssignedProviderProfileId == pid);
        filtered = request.AllocationState?.Trim().ToLowerInvariant() switch
        {
            "allocated"   => filtered.Where(b => b.AssignedProviderProfileId != null),
            "unallocated" => filtered.Where(b => b.AssignedProviderProfileId == null),
            _              => filtered,
        };
        var filteredList = filtered.ToList();

        var skip  = (request.Page - 1) * request.PageSize;
        var paged = filteredList.Skip(skip).Take(request.PageSize).ToList();

        return new CargoDryBatchListDto
        {
            Items = paged.Select(b => new CargoDryBatchDto
            {
                Id                        = b.Id,
                BatchCode                 = b.BatchCode,
                ProductCode               = b.ProductCode,
                ProductName               = prods.TryGetValue(b.ProductCode, out var p) ? p.Name : b.ProductCode,
                TotalKits                 = b.KitCount,
                GeneratedAt               = b.CreateDate?.ToString("O") ?? string.Empty,
                IsRevoked                 = b.IsRevoked,
                QrZipFileRef              = b.QrZipFileRef,
                ExcelFileRef              = b.ExcelFileRef,
                CreatedByAdminId          = b.CreatedByAdminId,
                BatchLabel                = b.BatchLabel,
                WarehouseCode             = b.WarehouseCode,
                // Phase 0 commercial fields
                AssignedProviderProfileId = b.AssignedProviderProfileId,
                CommercialModel           = b.CommercialModel,
                ConsignmentAgreementId    = b.ConsignmentAgreementId,
            }).ToList(),
            Total    = filteredList.Count,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
