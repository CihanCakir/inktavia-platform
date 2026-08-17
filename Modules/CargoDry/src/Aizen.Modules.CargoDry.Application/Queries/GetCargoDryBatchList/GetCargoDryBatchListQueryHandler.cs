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

        var skip  = (request.Page - 1) * request.PageSize;
        var paged = all.Skip(skip).Take(request.PageSize).ToList();

        return new CargoDryBatchListDto
        {
            Items = paged.Select(b => new CargoDryBatchDto
            {
                Id               = b.Id,
                BatchCode        = b.BatchCode,
                ProductCode      = b.ProductCode,
                ProductName      = prods.TryGetValue(b.ProductCode, out var p) ? p.Name : b.ProductCode,
                TotalKits        = b.KitCount,
                GeneratedAt      = b.CreateDate?.ToString("O") ?? string.Empty,
                IsRevoked        = b.IsRevoked,
                QrZipFileRef     = b.QrZipFileRef,
                ExcelFileRef     = b.ExcelFileRef,
                CreatedByAdminId = b.CreatedByAdminId,
            }).ToList(),
            Total    = all.Count,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
