using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;

namespace Aizen.Modules.FileStorage.Application.Queries.GetFileById;

[DocumentationInfo("Get file by ID query", "Returns the core file DTO for a given file ID.")]
public sealed class GetFileByIdQuery : AizenQuery<FileDto>
{
    public Guid FileId { get; }

    public GetFileByIdQuery(Guid fileId)
    {
        FileId = fileId;
    }
}
