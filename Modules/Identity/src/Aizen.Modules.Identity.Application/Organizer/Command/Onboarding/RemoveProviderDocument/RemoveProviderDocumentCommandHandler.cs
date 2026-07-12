using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Onboarding;
using Aizen.Modules.Identity.Domain.Enum;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Onboarding.RemoveProviderDocument;

/// <summary>
/// Removes (soft-deletes) a verification document from an organizer profile.
/// Validates profile ownership and that onboarding is not submitted.
/// </summary>
public sealed class RemoveProviderDocumentCommandHandler
    : AizenCommandHandler<RemoveProviderDocumentCommand, RemoveProviderDocumentResponse>
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<RemoveProviderDocumentCommandHandler> _logger;

    public RemoveProviderDocumentCommandHandler(IdentityDbContext db, ILogger<RemoveProviderDocumentCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public override async Task<RemoveProviderDocumentResponse?> Handle(
        RemoveProviderDocumentCommand request, CancellationToken ct)
    {
        // 1. Load profile with documents
        var profile = await _db.UserProfiles
            .Include(p => p.VerificationDocuments)
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId
                && p.RoleContext == WorkshopRoleContext.Organizer
                && !p.IsDeleted, ct);

        if (profile is null)
        {
            _logger.LogWarning("RemoveProviderDocument: profile {ProfileId} not found.", request.ProfileId);
            throw new AizenBusinessException("Profile not found.");
        }

        // 2. Guard: cannot remove after onboarding is submitted
        var onboarding = await _db.ProviderOnboarding
            .FirstOrDefaultAsync(o => o.ProfileId == request.ProfileId, ct);

        if (onboarding is not null && onboarding.Status == ProviderOnboardingStatus.Submitted)
        {
            _logger.LogWarning("RemoveProviderDocument: onboarding already submitted for profile {ProfileId}.", request.ProfileId);
            throw new AizenBusinessException("Cannot remove documents after onboarding has been submitted.");
        }

        // 3. Remove
        var removed = profile.RemoveVerificationDocument(request.FileId);
        if (!removed)
        {
            _logger.LogWarning("RemoveProviderDocument: file {FileId} not found on profile {ProfileId}.", request.FileId, request.ProfileId);
            throw new AizenBusinessException("Document not found on this profile.");
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("RemoveProviderDocument: removed file {FileId} from profile {ProfileId}.",
            request.FileId, request.ProfileId);

        return new RemoveProviderDocumentResponse { Success = true };
    }
}
