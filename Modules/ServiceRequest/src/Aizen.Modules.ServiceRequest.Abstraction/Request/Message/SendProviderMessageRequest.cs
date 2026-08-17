
namespace Aizen.Modules.ServiceRequest.Abstraction.Request.Message;

[DocumentationInfo("Send provider message request", "Provider sends a message scoped to a service request.")]
public sealed class SendProviderMessageRequest
{
    public string Content { get; set; } = default!;
    public Guid? AttachmentFileId { get; set; }
    public decimal? LocationLat { get; set; }
    public decimal? LocationLng { get; set; }
    public string? LocationLabel { get; set; }
}
