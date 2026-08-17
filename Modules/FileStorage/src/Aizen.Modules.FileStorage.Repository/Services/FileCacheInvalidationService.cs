using Aizen.Core.Cache.Abstraction;
using Aizen.Modules.FileStorage.Domain.Interface.Service;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File cache invalidation service", "Invalidates cached query results when file data changes.")]
public sealed class FileCacheInvalidationService : IFileCacheInvalidationService
{
    private readonly IAizenDistributedCache _cache;
    private readonly IFileCacheKeyService _cacheKeyService;

    public FileCacheInvalidationService(IAizenDistributedCache cache, IFileCacheKeyService cacheKeyService)
    {
        _cache = cache;
        _cacheKeyService = cacheKeyService;
    }

    public async Task InvalidateFileAsync(long fileId, Guid fileGuid, string fileCode, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(_cacheKeyService.FileById(fileId));
        await _cache.RemoveNoHash(_cacheKeyService.FileByGuid(fileGuid));
        await _cache.RemoveNoHash(_cacheKeyService.FileByCode(fileCode));
    }

    public async Task InvalidateOwnerFilesAsync(string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(_cacheKeyService.FilesByOwner(ownerModule, ownerEntityType, ownerEntityId));
    }

    public async Task InvalidateUploadSessionAsync(string uploadSessionCode, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(_cacheKeyService.UploadSession(uploadSessionCode));
    }

    public async Task InvalidateReadUrlAsync(Guid fileId, long? userId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(_cacheKeyService.ReadUrl(fileId, userId));
    }

    public async Task InvalidateProcessingJobsAsync(long fileId, CancellationToken ct = default)
    {
        await _cache.RemoveNoHash(_cacheKeyService.ProcessingJobs(fileId));
    }
}
