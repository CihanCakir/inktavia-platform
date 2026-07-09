using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    public class ApproveOrganizerProfileCommandHandler : AizenCommandHandler<ApproveOrganizerProfileCommand, VenueOrganizationRegistrationResponse>
    {
        private readonly IUserProfileRepository _profileRepo;
        private readonly UserManager<UserEntity> _userManager;
        private readonly IProviderKeycloakRoleSyncService _roleSync;
        private readonly ILogger<ApproveOrganizerProfileCommandHandler> _logger;

        public ApproveOrganizerProfileCommandHandler(
            IUserProfileRepository profileRepo,
            UserManager<UserEntity> userManager,
            IProviderKeycloakRoleSyncService roleSync,
            ILogger<ApproveOrganizerProfileCommandHandler> logger)
        {
            _profileRepo = profileRepo;
            _userManager = userManager;
            _roleSync = roleSync;
            _logger = logger;
        }

        public override async Task<VenueOrganizationRegistrationResponse?> Handle(ApproveOrganizerProfileCommand request, CancellationToken ct)
        {
            var profile = await _profileRepo.GetProfileByIdAsync(request.ProfileId)
                      ?? throw new AizenBusinessException(((int)AizenErrorCode.ProfileCreateFailed).ToString());

            if (profile.UserId != request.UserId || profile.RoleContext != WorkshopRoleContext.Organizer)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileStatusInvalidForAction).ToString());

            if (profile.ApprovalStatus == ApprovalStatus.Approved)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyApproved).ToString());
            if (profile.ApprovalStatus == ApprovalStatus.Rejected)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyRejected).ToString());

            // 1) Profil onayla
            profile.Approve(request.ReviewedBy);
            _profileRepo.UpdateProfileAsync(profile);

            // 2) Kullanıcı aktif profilini bu profile set et
            var user = await _userManager.FindByIdAsync(request.UserId.ToString())
                      ?? throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

            user.SetActiveProfile(profile.Id);
            await _userManager.UpdateAsync(user);

            // 3) Keycloak role sync (provider_pending → provider_user). Best-effort: never fail approval.
            try
            {
                await _roleSync.OnApprovedAsync(user.KeycloakSubjectId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keycloak role sync (approve) failed for user {UserId}.", request.UserId);
            }

            return new VenueOrganizationRegistrationResponse(
                Success: true,
                Action: "Approve",
                Context: WorkshopRoleContext.Organizer,
                UserId: request.UserId,
                ProfileId: request.ProfileId,
                NewStatus: profile.ApprovalStatus,
                ApprovedAt: profile.ApprovedAt,
                RejectedAt: profile.RejectedAt,
                RejectReason: profile.RejectReason,
                Message: "Organizer profile approved and set as active.",
                ReviewedBy: profile.ReviewedBy,
                ReviewedAt: profile.ReviewedAt?.ToString("O")
            );
        }
    }
}