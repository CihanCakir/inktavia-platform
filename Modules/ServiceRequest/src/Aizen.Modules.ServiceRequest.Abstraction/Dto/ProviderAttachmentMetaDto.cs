using Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Abstraction.Dto;

/// <summary>
/// Attachment metadata visible to providers. No signed URL, no object key.
/// Signed read URLs come from a separate endpoint (P4).
/// </summary>
public sealed class ProviderAttachmentMetaDto
{
    public long Id { get; set; }
    public Guid FileId { get; set; }
    public ServiceRequestAttachmentType AttachmentType { get; set; }
    public string? Title { get; set; }
    public DateTime CreatedAt { get; set; }
}
