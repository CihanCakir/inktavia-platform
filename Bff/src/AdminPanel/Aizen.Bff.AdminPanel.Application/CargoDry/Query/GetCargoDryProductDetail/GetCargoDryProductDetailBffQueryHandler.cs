using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.FileStorage.Abstraction.RemoteCall.File.Requests;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryProductDetail;

[DocumentationInfo("Get CargoDry product detail BFF query handler",
    "Returns a single product with operational kit statistics from the CargoDry admin catalog, plus the product media " +
    "(thumbnail + ordered gallery) resolved to presigned read URLs via FileStorage.")]
public sealed class GetCargoDryProductDetailBffQueryHandler
    : AizenQueryHandler<GetCargoDryProductDetailBffQuery, GetCargoDryProductDetailBffResponse?>
{
    private readonly ICargoDryRemoteCall    _remote;
    private readonly IFileStorageRemoteCall _fileStorage;

    public GetCargoDryProductDetailBffQueryHandler(
        ICargoDryRemoteCall remote, IFileStorageRemoteCall fileStorage)
    {
        _remote      = remote;
        _fileStorage = fileStorage;
    }

    public override async Task<GetCargoDryProductDetailBffResponse?> Handle(
        GetCargoDryProductDetailBffQuery request, CancellationToken ct)
    {
        var product = await _remote.GetProductDetailAsync(request.ProductCode, ct);
        if (product is null) return null;

        // Kit stats are now included in the module response — map them through if present.
        CargoDryProductKitStatsBffDto? kitStats = product.KitStats is { } ks
            ? new CargoDryProductKitStatsBffDto
            {
                TotalKitsIssued  = ks.TotalKitsIssued,
                ActiveKits       = ks.ActiveKits,
                ExpiredKits      = ks.ExpiredKits,
                RevokedKits      = ks.RevokedKits,
                RenewedKits      = ks.RenewedKits,
                AvgEfficiencyPct = ks.AvgEfficiencyPct,
                RenewalRatePct   = ks.RenewalRatePct,
                ExpiringIn30Days = ks.ExpiringIn30Days,
            }
            : null;

        // ── Resolve product media file ids → presigned read URLs (CargoDry supply flow) ──
        // FileStorage returns FileAccessUrlDto directly in AizenApiResponse<T> (NOT wrapped) — bind .Body.ReadUrl.
        var urlReq = new CreateFileReadUrlRemoteCallRequest { ExpiresIn = TimeSpan.FromMinutes(60) };

        string? thumbnailUrl = null;
        if (product.ThumbnailFileId is { } thumbId)
        {
            var r = await _fileStorage.CreateReadUrl(thumbId, urlReq);
            thumbnailUrl = r?.Body?.ReadUrl;
        }

        var images = new List<CargoDryProductImageBffDto>(product.ImageFileIds.Count);
        for (var i = 0; i < product.ImageFileIds.Count; i++)
        {
            var fileId = product.ImageFileIds[i];
            var r = await _fileStorage.CreateReadUrl(fileId, urlReq);
            images.Add(new CargoDryProductImageBffDto
            {
                FileId    = fileId,
                Url       = r?.Body?.ReadUrl,
                SortOrder = i,
            });
        }

        var dto = new CargoDryProductBffDto
        {
            Id              = product.Id,
            ProductCode     = product.ProductCode,
            Name            = product.Name,
            Description     = product.Description,
            ValidityDays    = product.ValidityDays,
            HasSmartDevice  = product.HasSmartDevice,
            DeviceType      = product.DeviceType,
            RetailPrice     = product.RetailPrice,
            CurrencyCode    = product.CurrencyCode,
            IsActive        = product.IsActive,
            CreatedAt       = product.CreatedAt,
            KitStats        = kitStats,
            ThumbnailFileId = product.ThumbnailFileId,
            ImageFileIds    = product.ImageFileIds,
            ThumbnailUrl    = thumbnailUrl,
            Images          = images,
        };

        return new GetCargoDryProductDetailBffResponse { Product = dto };
    }
}
