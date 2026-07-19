using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class AttachmentReadUrlBffResponse
{
    public string Url { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}

public sealed class GetAttachmentReadUrlBffQuery : AizenQuery<AttachmentReadUrlBffResponse>
{
    public long ServiceRequestId { get; init; }
    public Guid FileId { get; init; }
}
