using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer
{
    public class AddOrganizerVerificationDocumentCommandHandler
        : AizenCommandHandler<AddOrganizerVerificationDocumentCommand, AddVerificationDocumentResult>
    {
        private readonly IdentityDbContext _db;

        public AddOrganizerVerificationDocumentCommandHandler(IdentityDbContext db)
        {
            _db = db;
        }

        public override async Task<AddVerificationDocumentResult?> Handle(
            AddOrganizerVerificationDocumentCommand request, CancellationToken ct)
        {
            var profile = await _db.UserProfiles
                .Include(p => p.VerificationDocuments)
                .FirstOrDefaultAsync(p => p.Id == request.ProfileId
                    && p.UserId == request.UserId
                    && p.RoleContext == WorkshopRoleContext.Organizer
                    && !p.IsDeleted, ct)
                ?? throw new AizenBusinessException(((int)AizenErrorCode.NotFound).ToString());

            var document = VerificationDocumentEntity.Create(
                profileId: profile.Id,
                fileId: request.FileId,
                name: request.Name,
                documentType: request.DocumentType,
                format: request.Format,
                fileSizeDisplay: request.FileSizeDisplay,
                issuer: request.Issuer,
                uploadedByUserId: request.UploadedByUserId);

            profile.AddVerificationDocument(document);
            await _db.SaveChangesAsync(ct);

            return new AddVerificationDocumentResult(document.Id);
        }
    }
}
