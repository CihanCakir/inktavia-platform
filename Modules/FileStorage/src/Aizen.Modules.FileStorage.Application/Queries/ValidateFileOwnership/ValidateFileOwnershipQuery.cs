using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.Access;

namespace Aizen.Modules.FileStorage.Application.Queries.ValidateFileOwnership;

[DocumentationInfo("Validate file ownership query", "Checks whether a given module entity owns the specified file.")]
public sealed class ValidateFileOwnershipQuery : AizenQuery<FileValidationResultDto>
{
    public long FileId { get; }
    public ValidateFileOwnershipRequest Request { get; }

    public ValidateFileOwnershipQuery(long fileId, ValidateFileOwnershipRequest request)
    {
        FileId = fileId;
        Request = request;
    }
}
