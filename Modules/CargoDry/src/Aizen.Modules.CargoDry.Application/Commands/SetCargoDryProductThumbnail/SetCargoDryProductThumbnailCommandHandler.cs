using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Application.Commands.AddCargoDryProductImage;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.SetCargoDryProductThumbnail;

public sealed class SetCargoDryProductThumbnailCommandHandler
    : AizenCommandHandler<SetCargoDryProductThumbnailCommand, CargoDryProductImagesDto>
{
    private readonly ICargoDryProductRepository _products;

    public SetCargoDryProductThumbnailCommandHandler(ICargoDryProductRepository products)
        => _products = products;

    public override async Task<CargoDryProductImagesDto?> Handle(
        SetCargoDryProductThumbnailCommand request, CancellationToken ct)
    {
        var product = await _products.GetByCodeWithImagesAsync(request.ProductCode, ct)
            ?? throw new AizenBusinessException($"CargoDry product '{request.ProductCode}' not found.");

        product.SetThumbnail(request.FileId);
        await _products.SaveChangesAsync(ct);

        return CargoDryProductImageMapper.Map(product);
    }
}
