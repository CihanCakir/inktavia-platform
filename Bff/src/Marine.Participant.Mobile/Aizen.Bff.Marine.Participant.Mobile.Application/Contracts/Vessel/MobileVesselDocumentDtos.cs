namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;

/// <summary>A vessel document for the mobile manage surface (M4e). Codes (type) are the stored values; the
/// download URL is a freshly-resolved presigned FileStorage read URL (resolved per-doc by the BFF so the list
/// read stays on the page the module invalidates).</summary>
public sealed class MobileVesselDocumentDto
{
    public long Id { get; set; }
    public string DocumentTypeCode { get; set; } = default!;
    public string DocumentName { get; set; } = default!;
    public string? Status { get; set; }
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeInBytes { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>Presigned read URL (may be null if the file could not be resolved).</summary>
    public string? DownloadUrl { get; set; }
    public DateTime? DownloadUrlExpiresAt { get; set; }
}

/// <summary>Attach a completed client-side upload (fileId) to the vessel as a typed document. The bytes were PUT
/// directly to storage via /mobile/uploads — this carries only the fileId + document metadata.</summary>
public sealed class UploadMobileVesselDocumentRequest
{
    public Guid FileId { get; set; }
    public string DocumentTypeCode { get; set; } = default!;
    public string? DocumentName { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Confirmation of a document deletion.</summary>
public sealed class MobileVesselDocumentDeletedDto
{
    public long VesselId { get; set; }
    public long DocumentId { get; set; }
}
