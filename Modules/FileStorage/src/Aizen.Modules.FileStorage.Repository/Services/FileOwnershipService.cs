using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;
using Aizen.Modules.FileStorage.Domain.Entities.Access;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Domain.Interface.Service;
using Aizen.Modules.FileStorage.Repository.Persistence;

namespace Aizen.Modules.FileStorage.Repository.Services;

[DocumentationInfo("File ownership service", "Links files to owning module entities and validates ownership claims.")]
public sealed class FileOwnershipService : IFileOwnershipService
{
    private readonly FileStorageDbContext _db;
    private readonly IFileRepository _fileRepository;
    private readonly IFileOwnerReferenceRepository _ownerRefRepository;
    private readonly IFileCacheInvalidationService _cacheInvalidation;

    public FileOwnershipService(
        FileStorageDbContext db,
        IFileRepository fileRepository,
        IFileOwnerReferenceRepository ownerRefRepository,
        IFileCacheInvalidationService cacheInvalidation)
    {
        _db = db;
        _fileRepository = fileRepository;
        _ownerRefRepository = ownerRefRepository;
        _cacheInvalidation = cacheInvalidation;
    }

    public async Task<FileOwnerReferenceDto> LinkFileToOwnerAsync(long fileId, LinkFileToOwnerRequest request, long? linkedByUserId, CancellationToken cancellationToken = default)
    {
        var file = await _fileRepository.GetByIdAsync(fileId, cancellationToken)
            ?? throw new KeyNotFoundException($"File with id '{fileId}' not found.");

        var exists = await _ownerRefRepository.ExistsAsync(fileId, request.OwnerModule, request.OwnerEntityType, request.OwnerEntityId, cancellationToken);
        if (exists)
            throw new InvalidOperationException("File is already linked to this owner.");

        var reference = FileOwnerReferenceEntity.Create(fileId, request.OwnerModule, request.OwnerEntityType, request.OwnerEntityId, linkedByUserId);
        await _ownerRefRepository.AddAsync(reference, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        await _cacheInvalidation.InvalidateFileAsync(file.Id, file.PublicId ?? Guid.Empty, file.FileCode, cancellationToken);
        await _cacheInvalidation.InvalidateOwnerFilesAsync(request.OwnerModule, request.OwnerEntityType, request.OwnerEntityId, cancellationToken);

        return MapToDto(reference, file.PublicId ?? Guid.Empty);
    }

    public Task<bool> ValidateOwnershipAsync(long fileId, string ownerModule, string ownerEntityType, Guid ownerEntityId, CancellationToken cancellationToken = default)
        => _ownerRefRepository.ExistsAsync(fileId, ownerModule, ownerEntityType, ownerEntityId, cancellationToken);

    private static FileOwnerReferenceDto MapToDto(FileOwnerReferenceEntity reference, Guid fileGuid) => new()
    {
        FileId = fileGuid,
        OwnerModule = reference.OwnerModule,
        OwnerEntityType = reference.OwnerEntityType,
        OwnerEntityId = reference.OwnerEntityId,
        LinkedAt = reference.LinkedAt,
        LinkedByUserId = reference.LinkedByUserId,
        IsActive = reference.IsActive
    };
}
