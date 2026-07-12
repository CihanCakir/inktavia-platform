using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Files.CreateUploadSession;

public sealed class CreateUploadSessionCommand : AizenCommand<CreateUploadSessionBffResponse>
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public long Size { get; set; }
    public string Category { get; set; } = "Document";
}
