using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Repository.Identity.Service.Onboarding;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Venue
{
    /// <summary>
    /// Legacy handler used by the admin / internal API for venue documents.
    /// All validation, persistence, and file-claim logic is delegated to
    /// <see cref="IFileAttachmentValidationService"/> with <c>skipOwnershipCheck: true</c>
    /// and <c>roleContext: VenueOwner</c>.
    /// </summary>
    public class AddVenueVerificationDocumentCommandHandler
        : AizenCommandHandler<AddVenueVerificationDocumentCommand, AddVerificationDocumentResult>
    {
        private readonly IFileAttachmentValidationService _validationService;

        public AddVenueVerificationDocumentCommandHandler(
            IFileAttachmentValidationService validationService)
        {
            _validationService = validationService;
        }

        public override async Task<AddVerificationDocumentResult?> Handle(
            AddVenueVerificationDocumentCommand request, CancellationToken ct)
        {
            var document = await _validationService.ValidateAndAttachAsync(
                request.ProfileId, request.UserId, request.FileId,
                request.DocumentType, request.Issuer,
                actorIsAdmin: true, ct,
                roleContext: WorkshopRoleContext.VenueOwner);

            return new AddVerificationDocumentResult(document.Id);
        }
    }
}
