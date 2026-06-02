using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Domain.Documents;
using Aizen.Modules.FileStorage.Domain.Interface.Repository;
using Aizen.Modules.FileStorage.Repository.Mongo;
using Aizen.Modules.FileStorage.Repository.Persistence;
using MongoDB.Driver;

namespace Aizen.Modules.FileStorage.Repository.Repositories;

[DocumentationInfo("File metadata document repository", "MongoDB implementation of IFileMetadataDocumentRepository.")]
public sealed class FileMetadataDocumentRepository : IFileMetadataDocumentRepository
{
    private readonly IMongoCollection<FileRichMetadataDocument> _collection;

    public FileMetadataDocumentRepository(FileStorageMongoDbContext mongoDbContext)
    {
        _collection = mongoDbContext.Database.GetCollection<FileRichMetadataDocument>(
            FileStorageMongoCollectionNames.FileRichMetadata);
    }

    public Task<FileRichMetadataDocument?> GetByFileIdAsync(Guid fileId, CancellationToken ct = default)
        => _collection.Find(x => x.FileId == fileId).FirstOrDefaultAsync(ct)!;

    public async Task UpsertAsync(FileRichMetadataDocument document, CancellationToken ct = default)
    {
        var filter = Builders<FileRichMetadataDocument>.Filter.Eq(x => x.FileId, document.FileId);
        await _collection.ReplaceOneAsync(filter, document, new ReplaceOptions { IsUpsert = true }, ct);
    }
}
