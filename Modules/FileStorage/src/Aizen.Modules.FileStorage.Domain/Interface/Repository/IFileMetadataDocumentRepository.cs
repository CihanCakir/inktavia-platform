using Aizen.Modules.FileStorage.Domain.Documents;

namespace Aizen.Modules.FileStorage.Domain.Interface.Repository;

[DocumentationInfo("File metadata document repository interface", "Data access contract for MongoDB file metadata documents.")]
public interface IFileMetadataDocumentRepository
{
    Task<FileRichMetadataDocument?> GetByFileIdAsync(Guid fileId, CancellationToken ct = default);
    Task UpsertAsync(FileRichMetadataDocument document, CancellationToken ct = default);
}
