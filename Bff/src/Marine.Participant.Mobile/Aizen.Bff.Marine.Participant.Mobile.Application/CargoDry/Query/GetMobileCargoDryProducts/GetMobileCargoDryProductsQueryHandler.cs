using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>
/// Owner-safe product catalog. Reads the module catalog (owner-safe DTO — no commercial fields), then resolves each
/// distinct media file id to a presigned read URL via FileStorage (minted once per id, in parallel; a failed URL is
/// tolerated — left null / omitted, never throws). A transport failure on the catalog read surfaces as a clean
/// business error. Behind mobile auth; no owner scoping needed (the catalog is global + owner-safe).
/// </summary>
public sealed class GetMobileCargoDryProductsQueryHandler
    : AizenQueryHandler<GetMobileCargoDryProductsQuery, List<MobileCargoDryProductDto>>
{
    private static readonly TimeSpan ReadUrlTtl = TimeSpan.FromMinutes(15);

    private readonly ICargoDryRemoteCall _cargoDry;
    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<GetMobileCargoDryProductsQueryHandler> _logger;

    public GetMobileCargoDryProductsQueryHandler(
        ICargoDryRemoteCall cargoDry,
        IFileStorageRemoteCall fileStorage,
        ILogger<GetMobileCargoDryProductsQueryHandler> logger)
    {
        _cargoDry = cargoDry;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public override async Task<List<MobileCargoDryProductDto>> Handle(
        GetMobileCargoDryProductsQuery request, CancellationToken cancellationToken)
    {
        List<CargoDryProductCatalogDto> products;
        try
        {
            products = await _cargoDry.GetCatalogProducts() ?? new();
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "CargoDry catalog fetch failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Could not load the CargoDry product catalog.");
        }

        if (products.Count == 0)
            return new();

        // Mint each distinct file id → read URL once (parallel), tolerating individual failures.
        var fileIds = products
            .SelectMany(p => p.ImageFileIds.Concat(p.ThumbnailFileId.HasValue ? new[] { p.ThumbnailFileId.Value } : Array.Empty<Guid>()))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var urlMap = await ResolveReadUrlsAsync(fileIds);

        return products.Select(p => MobileCargoDryMapper.MapProduct(p, urlMap)).ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolveReadUrlsAsync(List<Guid> fileIds)
    {
        var pairs = await Task.WhenAll(fileIds.Select(async id =>
        {
            try
            {
                var url = await _fileStorage.CreateReadUrl(id, new CreateReadUrlRequest { ExpiresIn = ReadUrlTtl });
                var readUrl = url?.Body?.ReadUrl;
                return string.IsNullOrEmpty(readUrl) ? ((Guid, string)?)null : (id, readUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not mint a read-url for CargoDry product media {FileId}.", id);
                return null;
            }
        }));

        return pairs.Where(p => p.HasValue).Select(p => p!.Value).ToDictionary(p => p.Item1, p => p.Item2);
    }
}
