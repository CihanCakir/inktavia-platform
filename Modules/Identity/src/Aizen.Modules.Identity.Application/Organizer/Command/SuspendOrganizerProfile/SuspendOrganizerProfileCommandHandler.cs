using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.SuspendOrganizerProfile;

/// <summary>
/// Suspends an Organizer profile (ProfileStatus = Suspended) via the unit of work / repository, then syncs
/// Keycloak roles (remove provider_user, add provider_restricted, optional session revoke). Best-effort role
/// sync: failures are logged and never roll back the Identity status change.
/// </summary>
public sealed class SuspendOrganizerProfileCommandHandler
    : AizenCommandHandler<SuspendOrganizerProfileCommand, SuspendOrganizerProfileResponse>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;
    private readonly IProviderKeycloakRoleSyncService _roleSync;
    private readonly IAizenMessagePublisher _publisher;
    private readonly ILogger<SuspendOrganizerProfileCommandHandler> _logger;

    public SuspendOrganizerProfileCommandHandler(
        IAizenUnitOfWork<IdentityDbContext> uow,
        IProviderKeycloakRoleSyncService roleSync,
        IAizenMessagePublisher publisher,
        ILogger<SuspendOrganizerProfileCommandHandler> logger)
    {
        _uow = uow;
        _roleSync = roleSync;
        _publisher = publisher;
        _logger = logger;
    }

    public override async Task<SuspendOrganizerProfileResponse?> Handle(
        SuspendOrganizerProfileCommand request, CancellationToken cancellationToken)
    {
        var profileRepo = _uow.GetRepository<UserProfileEntity>();

        var profile = await profileRepo.FirstOrDefaultAsync(
            p => p.Id == request.ProfileId
                 && p.UserId == request.UserId
                 && p.RoleContext == WorkshopRoleContext.Organizer
                 && !p.IsDeleted)
            ?? throw new AizenBusinessException(((int)AizenErrorCode.ProfileStatusInvalidForAction).ToString());

        var alreadySuspended = profile.Status == ProfileStatus.Suspended;
        if (!alreadySuspended)
        {
            profile.Suspend(request.Reason);
            profileRepo.Update(profile);
        }

        var user = await _uow.GetRepository<UserEntity>().FirstOrDefaultAsync(u => u.Id == profile.UserId);

        try
        {
            await _roleSync.OnSuspendedAsync(user?.KeycloakSubjectId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Keycloak role sync (suspend) failed for user {UserId}.", request.UserId);
        }

        if (!alreadySuspended)
        {
            await _publisher.PublishAsync(new ProviderProfileSuspendedMessage
            {
                ProfileId = request.ProfileId,
                UserId = request.UserId,
                Email = user?.Email,
                Reason = request.Reason ?? "No reason provided",
                SuspendedAtUtc = DateTime.UtcNow,
            }, cancellationToken);
        }

        return new SuspendOrganizerProfileResponse(
            Success: true,
            UserId: request.UserId,
            ProfileId: request.ProfileId,
            ProfileStatus: profile.Status.ToString(),
            ApprovalStatus: profile.ApprovalStatus.ToString(),
            Message: alreadySuspended ? "Profile already suspended." : "Profile suspended.");
    }
}
