using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Message;

namespace Aizen.Bff.MarineProvider.Application.ServiceRequests;

public sealed class SendProviderMessageCommand : AizenCommand<SendServiceRequestMessageResponse>
{
    public long ServiceRequestId { get; init; }
    public string Content { get; init; } = default!;
    public Guid? AttachmentFileId { get; init; }
    public decimal? LocationLat { get; init; }
    public decimal? LocationLng { get; init; }
    public string? LocationLabel { get; init; }
}
