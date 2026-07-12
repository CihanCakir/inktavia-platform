using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Repository.Identity.Service.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer
{
    /// <summary>
    /// Legacy handler used by the admin / internal API.
    /// All validation, persistence, and file-claim logic is delegated to
    /// <see cref="IFileAttachmentValidationService"/> with <c>skipOwnershipCheck: true</c>
    /// because the admin user is not the file uploader.
    /// </summary>
    public class AddOrganizerVerificationDocumentCommandHandler
        : AizenCommandHandler<AddOrganizerVerificationDocumentCommand, AddVerificationDocumentResult>
    {
        private readonly IFileAttachmentValidationService _validationService;

        public AddOrganizerVerificationDocumentCommandHandler(
            IFileAttachmentValidationService validationService)
        {
            _validationService = validationService;
        }

        public override async Task<AddVerificationDocumentResult?> Handle(
            AddOrganizerVerificationDocumentCommand request, CancellationToken ct)
        {
            var document = await _validationService.ValidateAndAttachAsync(
                request.ProfileId, request.UserId, request.FileId,
                request.DocumentType, request.Issuer,
                skipOwnershipCheck: true, ct);

            return new AddVerificationDocumentResult(document.Id);
        }
    }
}
