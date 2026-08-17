using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;

[DocumentationInfo("Document upload URL BFF response", "Pre-signed PUT URL and session info returned after requesting an upload URL for a verification document.")]
public sealed class DocumentUploadUrlBffResponse
{
    public string? FileId { get; set; }              // FileStorage FileId (Guid as string)
    public string? UploadUrl { get; set; }           // Pre-signed PUT URL for direct browser→MinIO upload (TTL 5 min)
    public string? UploadSessionCode { get; set; }  // Required for CompleteUploadSession call
    public DateTime? ExpiresAt { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Register document BFF response", "Result of registering a verification document with Identity after upload completion.")]
public sealed class RegisterDocumentBffResponse
{
    public long DocumentId { get; set; }
    public string? FileId { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
