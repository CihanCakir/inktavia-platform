using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.ReactivateOrganizerProfile;

/// <summary>
/// Reactivates an Approved Organizer profile (ProfileStatus = Active) via the unit of work / repository, then
/// syncs Keycloak roles (add provider_user, remove provider_restricted). Never approves a Pending/Rejected
/// profile. Best-effort role sync: failures are logged and never roll back the Identity status change.
/// </summary>
public sealed class ReactivateOrganizerProfileCommandHandler
    : AizenCommandHandler<ReactivateOrganizerProfileCommand, ReactivateOrganizerProfileResponse>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;
    private readonly IProviderKeycloakRoleSyncService _roleSync;
    private readonly ILogger<ReactivateOrganizerProfileCommandHandler> _logger;

    public ReactivateOrganizerProfileCommandHandler(
        IAizenUnitOfWork<IdentityDbContext> uow,
        IProviderKeycloakRoleSyncService roleSync,
        ILogger<ReactivateOrganizerProfileCommandHandler> logger)
    {
        _uow = uow;
        _roleSync = roleSync;
        _logger = logger;
    }

    public override async Task<ReactivateOrganizerProfileResponse?> Handle(
        ReactivateOrganizerProfileCommand request, CancellationToken cancellationToken)
    {
        var profileRepo = _uow.GetRepository<UserProfileEntity>();

        var profile = await profileRepo.FirstOrDefaultAsync(
            p => p.Id == request.ProfileId
                 && p.UserId == request.UserId
                 && p.RoleContext == WorkshopRoleContext.Organizer
                 && !p.IsDeleted)
            ?? throw new AizenBusinessException(((int)AizenErrorCode.ProfileStatusInvalidForAction).ToString());

        var alreadyActive = profile.Status == ProfileStatus.Active;
        if (!alreadyActive)
        {
            // Throws when ApprovalStatus != Approved (never approves implicitly).
            profile.Reactivate();
            profileRepo.Update(profile);
        }

        var user = await _uow.GetRepository<UserEntity>().FirstOrDefaultAsync(u => u.Id == profile.UserId);

        try
        {
            await _roleSync.OnReactivatedAsync(user?.KeycloakSubjectId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak role sync (reactivate) failed for user {UserId}.", request.UserId);
        }

        return new ReactivateOrganizerProfileResponse(
            Success: true,
            UserId: request.UserId,
            ProfileId: request.ProfileId,
            ProfileStatus: profile.Status.ToString(),
            ApprovalStatus: profile.ApprovalStatus.ToString(),
            Message: alreadyActive ? "Profile already active." : "Profile reactivated.");
    }
}
