using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Repository.Identity.Service.Onboarding;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.AttachProviderDocument;

/// <summary>
/// Attaches a verified file to an organizer profile (BFF path).
/// All validation, persistence, and file-claim logic is delegated to
/// <see cref="IFileAttachmentValidationService"/>.
/// </summary>
public sealed class AttachProviderDocumentCommandHandler
    : AizenCommandHandler<AttachProviderDocumentCommand, AttachProviderDocumentResponse>
{
    private readonly IFileAttachmentValidationService _validationService;

    public AttachProviderDocumentCommandHandler(IFileAttachmentValidationService validationService)
    {
        _validationService = validationService;
    }

    public override async Task<AttachProviderDocumentResponse?> Handle(
        AttachProviderDocumentCommand request, CancellationToken ct)
    {
        var document = await _validationService.ValidateAndAttachAsync(
            request.ProfileId, request.UserId, request.FileId,
            request.DocumentType, request.Issuer,
            skipOwnershipCheck: false, ct);

        return new AttachProviderDocumentResponse
        {
            DocumentId = document.Id,
            FileId = request.FileId,
            Success = true,
            Document = new ProviderDocumentDto
            {
                FileId = document.FilePublicId ?? Guid.Empty,
                FileName = document.Name,
                ContentType = document.ContentType ?? document.Format,
                SizeInBytes = document.SizeInBytes,
                DocumentType = document.DocumentType,
                Issuer = document.Issuer,
                UploadedAt = document.UploadedAt,
            },
        };
    }
}
