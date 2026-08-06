using Aizen.Bff.AdminPanel.Application.ProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ProfileApprovals.Command;

[DocumentationInfo("Request organizer document upload URL BFF command", "Requests a pre-signed PUT URL from FileStorage so the browser can upload a verification document directly.")]
public sealed class RequestOrganizerDocumentUploadUrlBffCommand : AizenCommand<DocumentUploadUrlBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }
    public string FileName { get; }
    public string ContentType { get; }
    public long FileSizeBytes { get; }
    public string DocumentType { get; }

    public RequestOrganizerDocumentUploadUrlBffCommand(
        long userId, long profileId,        string fileName, string contentType, long fileSizeBytes, string documentType)
    {
        UserId = userId;
        ProfileId = profileId;
        FileName = fileName;
        ContentType = contentType;
        FileSizeBytes = fileSizeBytes;
        DocumentType = documentType;
    }
}
