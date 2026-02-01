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
    public class RejectOrganizerProfileCommandHandler : AizenCommandHandler<RejectOrganizerProfileCommand, VenueOrganizationRegistrationResponse>
    {
        private readonly IUserProfileRepository _profileRepo;
        private readonly UserManager<UserEntity> _userManager;

        public RejectOrganizerProfileCommandHandler(IUserProfileRepository profileRepo, UserManager<UserEntity> userManager)
        {
            _profileRepo = profileRepo;
            _userManager = userManager;
        }
        public override async Task<VenueOrganizationRegistrationResponse?> Handle(RejectOrganizerProfileCommand request, CancellationToken ct)
        {
            var profile = await _profileRepo.GetProfileByIdAsync(request.ProfileId)
                       ?? throw new AizenBusinessException(((int)AizenErrorCode.ProfileCreateFailed).ToString());

            if (profile.UserId != request.UserId || profile.RoleContext != WorkshopRoleContext.Organizer)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileStatusInvalidForAction).ToString());

            // Aktif profili reddetme koruması (olası edge-case)
            var user = await _userManager.FindByIdAsync(request.UserId.ToString())
                      ?? throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

            if (user.ActiveProfileId == profile.Id)
                throw new AizenBusinessException(((int)AizenErrorCode.CannotRejectActiveProfile).ToString());

            // Durum kontrolü
            if (profile.ApprovalStatus == ApprovalStatus.Approved)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyApproved).ToString());
            if (profile.ApprovalStatus == ApprovalStatus.Rejected)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyRejected).ToString());

            // 1) Profil reddet
            profile.Reject(request.Reason ?? "No reason provided");
            _profileRepo.UpdateProfileAsync(profile);

            return new VenueOrganizationRegistrationResponse(
                Success: true,
                Action: "Reject",
                Context: WorkshopRoleContext.Organizer,
                UserId: request.UserId,
                ProfileId: request.ProfileId,
                NewStatus: profile.ApprovalStatus,
                ApprovedAt: profile.ApprovedAt,
                RejectedAt: profile.RejectedAt,
                RejectReason: profile.RejectReason,
                Message: "Organizer profile rejected."
            );
        }
    }
}
