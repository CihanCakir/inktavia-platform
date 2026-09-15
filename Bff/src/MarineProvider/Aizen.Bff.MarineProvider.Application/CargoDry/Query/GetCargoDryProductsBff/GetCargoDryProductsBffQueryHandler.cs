using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Bff.MarineProvider.Application.CargoDry.Dto;
using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Bff.MarineProvider.Application.Common.Services;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Bff.MarineProvider.Application.CargoDry;

public sealed class GetCargoDryProductsBffQueryHandler
    : AizenQueryHandler<GetCargoDryProductsBffQuery, List<CargoDryProviderProductOptionBffDto>>
{
    private readonly IProviderProfileResolver      _resolver;
    private readonly IProviderIdentityHolder       _h;
    private readonly ICargoDryRemoteCall           _c;
    private readonly ICargoDryProductMediaEnricher _media;

    public GetCargoDryProductsBffQueryHandler(
        IProviderProfileResolver r, IProviderIdentityHolder h, ICargoDryRemoteCall c,
        ICargoDryProductMediaEnricher media)
    {
        _resolver = r; _h = h; _c = c; _media = media;
    }

    public override async Task<List<CargoDryProviderProductOptionBffDto>?> Handle(
        GetCargoDryProductsBffQuery q, CancellationToken ct)
    {
        await _resolver.ResolveAsync(ct);
        if (_h.ProfileId is null or 0) throw new AizenBusinessException("Provider identity could not be resolved.");

        // Source the stock-request picker options from the (media-aware) active catalog so each option can carry a
        // thumbnail, matching the module /products contract (active products only).
        var active = ((await _c.GetCatalog()).Body ?? new List<CargoDryProductDto>())
            .Where(p => p.IsActive)
            .ToList();

        var urlMap = await _media.ResolveReadUrlsAsync(
            active.Where(p => p.ThumbnailFileId is not null).Select(p => p.ThumbnailFileId!.Value), ct);

        return active.Select(p => new CargoDryProviderProductOptionBffDto
        {
            ProductCode  = p.ProductCode,
            ProductName  = p.Name,
            ThumbnailUrl = p.ThumbnailFileId is { } tid && urlMap.TryGetValue(tid, out var turl) ? turl : null,
        }).ToList();
    }
}
