using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Request.Document;

[DocumentationInfo("Add vessel document request", "Input model for attaching an official document to a vessel.")]
public sealed class AddVesselDocumentRequest
{
    public string DocumentTypeCode { get; set; } = default!;
    public string DocumentName { get; set; } = default!;
    public string? FileId { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public string? MimeType { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}
