using Aizen.Bff.MarineProvider.Application.Common.RemoteClients;
using Aizen.Modules.FileStorage.Abstraction.Request.File;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.MarineProvider.Application.Common.Services;

/// <summary>
/// Resolves CargoDry product file ids into presigned FileStorage read URLs for provider read paths (catalog +
/// SR-detail product block). Mirrors the mobile BFF pattern: per-id client-side fan-out, fully failure-tolerant —
/// a missing/objectless file id (the SEED-400 bug class) is dropped, never surfaced as an error, so callers get
/// HTTP 200 with a null thumbnail / shorter gallery and the FE renders its own placeholder.
/// </summary>
public interface ICargoDryProductMediaEnricher
{
    Task<IReadOnlyDictionary<Guid, string>> ResolveReadUrlsAsync(IEnumerable<Guid> fileIds, CancellationToken ct);
}

public sealed class CargoDryProductMediaEnricher : ICargoDryProductMediaEnricher
{
    private static readonly TimeSpan ReadUrlTtl = TimeSpan.FromMinutes(15);

    private readonly IFileStorageRemoteCall _fileStorage;
    private readonly ILogger<CargoDryProductMediaEnricher> _logger;

    public CargoDryProductMediaEnricher(
        IFileStorageRemoteCall fileStorage, ILogger<CargoDryProductMediaEnricher> logger)
    {
        _fileStorage = fileStorage;
        _logger      = logger;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ResolveReadUrlsAsync(
        IEnumerable<Guid> fileIds, CancellationToken ct)
    {
        var distinct = fileIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (distinct.Count == 0) return new Dictionary<Guid, string>();

        var pairs = await Task.WhenAll(distinct.Select(async id =>
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
