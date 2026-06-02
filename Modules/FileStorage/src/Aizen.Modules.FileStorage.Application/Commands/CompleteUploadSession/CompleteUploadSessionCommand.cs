using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.File;
using Aizen.Modules.FileStorage.Abstraction.Model;
using Aizen.Modules.FileStorage.Abstraction.Request.UploadSession;

namespace Aizen.Modules.FileStorage.Application.Commands.CompleteUploadSession;

[DocumentationInfo("Complete upload session command", "Marks an S3 upload session as completed and finalizes the file record.")]
public sealed class CompleteUploadSessionCommand : AizenCommand<FileDto>
{
    public string UploadSessionCode { get; set; } = default!;
    public CompleteUploadSessionRequest Request { get; set; } = default!;
    public long? UserId { get; set; }
}
