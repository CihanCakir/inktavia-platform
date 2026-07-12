using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer
{
    public class AddOrganizerVerificationDocumentCommand : AizenCommand<AddVerificationDocumentResult>
    {
        public long UserId { get; }
        public long ProfileId { get; }
        public Guid FileId { get; }
        public string DocumentType { get; }
        public string? Issuer { get; }

        public AddOrganizerVerificationDocumentCommand(
            long userId,
            long profileId,
            Guid fileId,
            string documentType,
            string? issuer)
        {
            UserId = userId;
            ProfileId = profileId;
            FileId = fileId;
            DocumentType = documentType;
            Issuer = issuer;
        }
    }

    public record AddVerificationDocumentResult(long DocumentId);
}
