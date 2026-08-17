using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

/// <summary>
/// Step 1 of the vessel file upload flow (B3/B4): a pre-signed PUT URL + session so the browser uploads the file
/// directly to storage, then calls the register/replace endpoint with the returned FileId + UploadSessionCode.
/// </summary>
[DocumentationInfo("Vessel file upload URL BFF response", "Pre-signed PUT URL + session info for a vessel document/media upload.")]
public sealed class VesselFileUploadUrlBffResponse
{
    public string? FileId { get; set; }              // FileStorage FileId (Guid as string)
    public string? UploadUrl { get; set; }           // Pre-signed PUT URL for direct browser→storage upload
    public string? UploadSessionCode { get; set; }   // Required for the register/replace step
    public DateTime? ExpiresAt { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
