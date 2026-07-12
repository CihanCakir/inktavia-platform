using Aizen.Bff.MarineProvider.Application.Contracts.Files;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Files.CompleteUpload;

public sealed class CompleteUploadCommand : AizenCommand<CompleteUploadBffResponse>
{
    public Guid FileId { get; set; }
    public string UploadSessionCode { get; set; } = default!;
}
