using Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfileApprovals.Command;

[DocumentationInfo("Register organizer verification document BFF command", "Completes an upload session with FileStorage, then registers the document FileId with Identity.")]
public sealed class RegisterOrganizerVerificationDocumentCommand : AizenCommand<RegisterDocumentBffResponse>
{
    public long UserId { get; }
    public long ProfileId { get; }
    public string FileId { get; }               // FileStorage FileId (Guid as string)
    public string UploadSessionCode { get; }
    public string DocumentType { get; }
    public string Name { get; }
    public string? Format { get; }
    public string? FileSizeDisplay { get; }
    public string? Issuer { get; }

    public RegisterOrganizerVerificationDocumentCommand(
        long userId, long profileId,        string fileId, string uploadSessionCode,
        string documentType, string name,
        string? format, string? fileSizeDisplay, string? issuer)
    {
        UserId = userId;
        ProfileId = profileId;
        FileId = fileId;
        UploadSessionCode = uploadSessionCode;
        DocumentType = documentType;
        Name = name;
        Format = format;
        FileSizeDisplay = fileSizeDisplay;
        Issuer = issuer;
    }
}
