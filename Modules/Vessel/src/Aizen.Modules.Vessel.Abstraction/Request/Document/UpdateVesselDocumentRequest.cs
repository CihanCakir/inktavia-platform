using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Request.Document;

[DocumentationInfo("Update vessel document request", "Input model for updating a vessel document's metadata.")]
public sealed class UpdateVesselDocumentRequest
{
    public string DocumentName { get; set; } = default!;
    public string DocumentTypeCode { get; set; } = default!;
    public Guid? FileId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

