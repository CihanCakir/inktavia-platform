using Aizen.Core.CQRS.Message;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Venue
{
    public class AddVenueVerificationDocumentCommand : AizenCommand<AddVerificationDocumentResult>
    {
        public long UserId { get; }
        public long ProfileId { get; }
        public long FileId { get; }
        public string Name { get; }
        public string DocumentType { get; }
        public string? Format { get; }
        public string? FileSizeDisplay { get; }
        public string? Issuer { get; }
        public long UploadedByUserId { get; }

        public AddVenueVerificationDocumentCommand(
            long userId,
            long profileId,
            long fileId,
            string name,
            string documentType,
            string? format,
            string? fileSizeDisplay,
            string? issuer,
            long uploadedByUserId)
        {
            UserId = userId;
            ProfileId = profileId;
            FileId = fileId;
            Name = name;
            DocumentType = documentType;
            Format = format;
            FileSizeDisplay = fileSizeDisplay;
            Issuer = issuer;
            UploadedByUserId = uploadedByUserId;
        }
    }
}
