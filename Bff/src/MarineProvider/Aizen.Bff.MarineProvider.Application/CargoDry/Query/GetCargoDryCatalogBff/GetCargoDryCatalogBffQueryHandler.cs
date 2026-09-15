using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.CargoDry.Dto;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDryCatalogBffQueryHandler
    : AizenQueryHandler<GetCargoDryCatalogBffQuery, List<CargoDryProviderCatalogItemBffDto>>
{
    private readonly IProviderProfileResolver       _resolver;
    private readonly IProviderIdentityHolder        _h;
    private readonly ICargoDryRemoteCall            _c;
    private readonly ICargoDryProductMediaEnricher  _media;

    public GetCargoDryCatalogBffQueryHandler(
        IProviderProfileResolver r, IProviderIdentityHolder h, ICargoDryRemoteCall c,
        ICargoDryProductMediaEnricher media)
    {
        _resolver = r; _h = h; _c = c; _media = media;
    }

    public override async Task<List<CargoDryProviderCatalogItemBffDto>?> Handle(
        GetCargoDryCatalogBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        var products = (await _c.GetCatalog()).Body ?? new List<CargoDryProductDto>();

        var fileIds = products
            .SelectMany(p => p.ImageFileIds.Concat(p.ThumbnailFileId is { } t ? new[] { t } : Array.Empty<Guid>()));
        var urlMap = await _media.ResolveReadUrlsAsync(fileIds, ct);

        return products.Select(p => new CargoDryProviderCatalogItemBffDto
        {
            Id                     = p.Id,
            ProductCode            = p.ProductCode,
            Name                   = p.Name,
            Description            = p.Description,
            ValidityDays           = p.ValidityDays,
            HasSmartDevice         = p.HasSmartDevice,
            DeviceType             = p.DeviceType,
            RetailPrice            = p.RetailPrice,
            CurrencyCode           = p.CurrencyCode,
            IsActive               = p.IsActive,
            WholesalePrice         = p.WholesalePrice,
            ConsignmentPrice       = p.ConsignmentPrice,
            ProviderCommissionRate = p.ProviderCommissionRate,
            ThumbnailUrl           = p.ThumbnailFileId is { } tid && urlMap.TryGetValue(tid, out var turl) ? turl : null,
            ImageUrls              = p.ImageFileIds.Where(urlMap.ContainsKey).Select(id => urlMap[id]).ToList(),
        }).ToList();
    }
}
