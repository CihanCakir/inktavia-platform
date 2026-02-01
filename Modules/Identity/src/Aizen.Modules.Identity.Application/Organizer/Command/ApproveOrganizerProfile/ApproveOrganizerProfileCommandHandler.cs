using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    public class ApproveOrganizerProfileCommandHandler : AizenCommandHandler<ApproveOrganizerProfileCommand, VenueOrganizationRegistrationResponse>
    {
        private readonly IUserProfileRepository _profileRepo;
        private readonly UserManager<UserEntity> _userManager;

        public ApproveOrganizerProfileCommandHandler(IUserProfileRepository profileRepo, UserManager<UserEntity> userManager)
        {
            _profileRepo = profileRepo;
            _userManager = userManager;
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
            profile.Approve();
            _profileRepo.UpdateProfileAsync(profile);

            // 2) Kullanıcı aktif profilini bu profile set et
            var user = await _userManager.FindByIdAsync(request.UserId.ToString())
                      ?? throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

            user.SetActiveProfile(profile.Id);
            await _userManager.UpdateAsync(user);

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
                Message: "Organizer profile approved and set as active."
            );
        }
    }
}