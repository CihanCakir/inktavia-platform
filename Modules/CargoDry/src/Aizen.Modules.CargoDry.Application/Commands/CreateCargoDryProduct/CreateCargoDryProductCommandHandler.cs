using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Commands.CreateCargoDryProduct;

public sealed class CreateCargoDryProductCommandHandler
    : AizenCommandHandler<CreateCargoDryProductCommand, CargoDryProductDto>
{
    private readonly ICargoDryProductRepository _products;
    private readonly IAizenDistributedCache     _cache;

    public CreateCargoDryProductCommandHandler(
        ICargoDryProductRepository products,
        IAizenDistributedCache cache)
    {
        _products = products;
        _cache    = cache;
    }

    public override async Task<CargoDryProductDto> Handle(
        CreateCargoDryProductCommand request, CancellationToken ct)
    {
        var exists = await _products.ExistsByCodeAsync(request.ProductCode, ct);
        if (exists)
            throw new InvalidOperationException(
                $"A product with code '{request.ProductCode}' already exists.");

        var entity = CargoDryProductEntity.Create(
            productCode:    request.ProductCode,
            name:           request.Name,
            description:    request.Description,
            validityDays:   request.ValidityDays,
            retailPrice:    request.RetailPrice,
            currencyCode:   request.CurrencyCode,
            hasSmartDevice: request.HasSmartDevice,
            deviceType:     request.DeviceType);

        await _products.AddAsync(entity, ct);

        // Invalidate the cached product list
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
