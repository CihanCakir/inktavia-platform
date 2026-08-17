using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.UpdateCargoDryProduct;

public sealed class UpdateCargoDryProductCommandHandler
    : AizenCommandHandler<UpdateCargoDryProductCommand, CargoDryProductDto>
{
    private readonly ICargoDryProductRepository _products;
    private readonly IAizenDistributedCache     _cache;

    public UpdateCargoDryProductCommandHandler(
        ICargoDryProductRepository products,
        IAizenDistributedCache cache)
    {
        _products = products;
        _cache    = cache;
    }

    public override async Task<CargoDryProductDto> Handle(
        UpdateCargoDryProductCommand request, CancellationToken ct)
    {
        var entity = await _products.GetByCodeAsync(request.ProductCode, ct)
            ?? throw new InvalidOperationException($"Product '{request.ProductCode}' not found.");

        entity.Update(
            name:           request.Name,
            description:    request.Description,
            validityDays:   request.ValidityDays,
            retailPrice:    request.RetailPrice,
            currencyCode:   request.CurrencyCode,
            hasSmartDevice: request.HasSmartDevice,
            deviceType:     request.DeviceType);

        if (request.IsActive) entity.Activate();
        else                  entity.Deactivate();

        await _products.SaveChangesAsync(ct);

        // Invalidate product list cache
        await _cache.RemoveAsync<List<CargoDryProductDto>>("cargodry:products:all", ct);

        return new CargoDryProductDto
        {
            Id             = entity.Id,
            ProductCode    = entity.ProductCode,
            Name           = entity.Name,
            Description    = entity.Description,
            ValidityDays   = entity.ValidityDays,
            HasSmartDevice = entity.HasSmartDevice,
            DeviceType     = entity.DeviceType,
            RetailPrice    = entity.RetailPrice,
            CurrencyCode   = entity.CurrencyCode,
            IsActive       = entity.IsActive,
            CreatedAt      = entity.CreateDate?.ToString("O"),
        };
    }
}
