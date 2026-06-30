using Aizen.Core.CQRS.Message;
using Aizen.Modules.FileStorage.Abstraction.Dto.Access;
using Aizen.Modules.FileStorage.Abstraction.Request.File;

namespace Aizen.Modules.FileStorage.Application.Commands.CreateReadUrl;

[DocumentationInfo("Create read URL command", "Generates a pre-signed S3 URL to read/download a file.")]
public sealed class CreateReadUrlCommand : AizenCommand<FileAccessUrlDto>
{
    public long FileId { get; set; }
    public CreateReadUrlRequest Request { get; set; } = default!;
}
