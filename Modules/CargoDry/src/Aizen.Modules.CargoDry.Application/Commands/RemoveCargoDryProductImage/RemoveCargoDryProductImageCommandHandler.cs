using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Application.Commands.AddCargoDryProductImage;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.RemoveCargoDryProductImage;

public sealed class RemoveCargoDryProductImageCommandHandler
    : AizenCommandHandler<RemoveCargoDryProductImageCommand, CargoDryProductImagesDto>
{
    private readonly ICargoDryProductRepository _products;

    public RemoveCargoDryProductImageCommandHandler(ICargoDryProductRepository products)
        => _products = products;

    public override async Task<CargoDryProductImagesDto?> Handle(
        RemoveCargoDryProductImageCommand request, CancellationToken ct)
    {
        var product = await _products.GetByCodeWithImagesAsync(request.ProductCode, ct)
            ?? throw new AizenBusinessException($"CargoDry product '{request.ProductCode}' not found.");

        var removed = product.RemoveImage(request.FileId);
        if (removed is not null)
            _products.RemoveImage(removed);   // explicit orphan delete

        await _products.SaveChangesAsync(ct);

        return CargoDryProductImageMapper.Map(product);
    }
}
