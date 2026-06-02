using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Model;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileById;

[DocumentationInfo("Get file by ID query", "Returns the core file DTO for a given file ID.")]
public sealed class GetFileByIdQuery : AizenQuery<FileDto>
{
    public long FileId { get; }

    public GetFileByIdQuery(long fileId)
    {
        FileId = fileId;
    }
}
