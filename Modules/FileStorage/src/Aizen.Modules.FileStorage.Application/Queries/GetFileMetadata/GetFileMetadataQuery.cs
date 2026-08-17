using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileMetadata;

[DocumentationInfo("Get file metadata query", "Returns extended file metadata including owner references for a given file ID.")]
public sealed class GetFileMetadataQuery : AizenQuery<FileMetadataDto>
{
    public Guid FileId { get; }

    public GetFileMetadataQuery(Guid fileId)
    {
        FileId = fileId;
    }
}
