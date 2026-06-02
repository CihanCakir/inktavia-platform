using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.UploadSession;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

namespace Aizen.Modules.FileStorage.Application.Commands.CreateUploadSession;

[DocumentationInfo("Create upload session command", "Initiates a new S3 pre-signed upload session for a file.")]
public sealed class CreateUploadSessionCommand : AizenCommand<FileUploadSessionDto>
{
    public CreateUploadSessionRequest Request { get; set; } = default!;
}
