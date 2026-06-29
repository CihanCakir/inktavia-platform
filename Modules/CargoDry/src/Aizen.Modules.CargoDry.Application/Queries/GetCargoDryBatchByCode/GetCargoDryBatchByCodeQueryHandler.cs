using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryBatchByCode;

public sealed class GetCargoDryBatchByCodeQueryHandler
    : AizenQueryHandler<GetCargoDryBatchByCodeQuery, CargoDryBatchDto?>
{
    private readonly ICargoDryBatchRepository   _batches;
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryBatchByCodeQueryHandler(
        ICargoDryBatchRepository batches,
        ICargoDryProductRepository products)
    {
        _batches  = batches;
        _products = products;
    }

    public override async Task<CargoDryBatchDto?> Handle(
        GetCargoDryBatchByCodeQuery request, CancellationToken ct)
    {
        var batch = await _batches.GetByCodeAsync(request.BatchCode, ct);
        if (batch is null) return null;

        var prods = (await _products.GetAllAsync(ct)).ToDictionary(p => p.ProductCode);

        return new CargoDryBatchDto
        {
            Id               = batch.Id,
            BatchCode        = batch.BatchCode,
            ProductCode      = batch.ProductCode,
            ProductName      = prods.TryGetValue(batch.ProductCode, out var p) ? p.Name : batch.ProductCode,
            TotalKits        = batch.KitCount,
            GeneratedAt      = batch.CreateDate?.ToString("O") ?? string.Empty,
            IsRevoked        = batch.IsRevoked,
            QrZipFileRef     = batch.QrZipFileRef,
            ExcelFileRef     = batch.ExcelFileRef,
            CreatedByAdminId = batch.CreatedByAdminId,
        };
    }
}
