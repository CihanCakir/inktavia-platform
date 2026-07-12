using Aizen.Core.CQRS.Message;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Venue
{
    public class AddVenueVerificationDocumentCommand : AizenCommand<AddVerificationDocumentResult>
    {
        public long UserId { get; }
        public long ProfileId { get; }
        public Guid FileId { get; }
        public string DocumentType { get; }
        public string? Issuer { get; }

        public AddVenueVerificationDocumentCommand(
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
}
