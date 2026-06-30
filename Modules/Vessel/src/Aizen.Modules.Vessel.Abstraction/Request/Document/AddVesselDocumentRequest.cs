
namespace Aizen.Modules.Vessel.Abstraction.Request.Document;

[DocumentationInfo("Add vessel document request", "Input model for attaching an official document to a vessel.")]
public sealed class AddVesselDocumentRequest
{
    public string DocumentTypeCode { get; set; } = default!;
    public string DocumentName { get; set; } = default!;
    public Guid? FileId { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
    public bool DeleteFileFromStorage { get; set; } = false;
}

