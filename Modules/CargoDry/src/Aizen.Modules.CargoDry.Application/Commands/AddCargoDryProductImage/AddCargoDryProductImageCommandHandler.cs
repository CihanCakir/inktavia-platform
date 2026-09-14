using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.AddCargoDryProductImage;

public sealed class AddCargoDryProductImageCommandHandler
    : AizenCommandHandler<AddCargoDryProductImageCommand, CargoDryProductImagesDto>
{
    private readonly ICargoDryProductRepository _products;

    public AddCargoDryProductImageCommandHandler(ICargoDryProductRepository products)
        => _products = products;

    public override async Task<CargoDryProductImagesDto?> Handle(
        AddCargoDryProductImageCommand request, CancellationToken ct)
    {
        var product = await _products.GetByCodeWithImagesAsync(request.ProductCode, ct)
            ?? throw new AizenBusinessException($"CargoDry product '{request.ProductCode}' not found.");

        product.AddImage(request.FileId);
        await _products.SaveChangesAsync(ct);

        return CargoDryProductImageMapper.Map(product);
    }
}

/// <summary>Shared mapper: product aggregate → media DTO (thumbnail + ordered gallery).</summary>
internal static class CargoDryProductImageMapper
{
    public static CargoDryProductImagesDto Map(CargoDryProductEntity p) => new()
    {
        ThumbnailFileId = p.ThumbnailFileId,
        Images = p.Images
            .OrderBy(i => i.SortOrder)
            .Select(i => new CargoDryProductImageItemDto { FileId = i.FileId, SortOrder = i.SortOrder })
            .ToList(),
    };
}
