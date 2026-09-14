using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryCatalogProducts;

/// <summary>
/// Maps active products (+ ordered gallery images) to the owner-safe <see cref="CargoDryProductCatalogDto"/>. No
/// commercial fields are projected — the DTO physically cannot carry them, so wholesale/consignment/commission never
/// leave the module on this path.
/// </summary>
public sealed class GetCargoDryCatalogProductsQueryHandler
    : AizenQueryHandler<GetCargoDryCatalogProductsQuery, List<CargoDryProductCatalogDto>>
{
    private readonly ICargoDryProductRepository _products;

    public GetCargoDryCatalogProductsQueryHandler(ICargoDryProductRepository products) => _products = products;

    public override async Task<List<CargoDryProductCatalogDto>> Handle(
        GetCargoDryCatalogProductsQuery request, CancellationToken ct)
    {
        var products = await _products.GetAllActiveWithImagesAsync(ct);

        return products.Select(p => new CargoDryProductCatalogDto
        {
            Id              = p.Id,
            ProductCode     = p.ProductCode,
            Name            = p.Name,
            Description     = p.Description,
            ValidityDays    = p.ValidityDays,
            HasSmartDevice  = p.HasSmartDevice,
            DeviceType      = p.DeviceType,
            RetailPrice     = p.RetailPrice,
            CurrencyCode    = p.CurrencyCode,
            ThumbnailFileId = p.ThumbnailFileId,
            ImageFileIds    = p.Images.OrderBy(i => i.SortOrder).Select(i => i.FileId).ToList(),
        }).ToList();
    }
}
