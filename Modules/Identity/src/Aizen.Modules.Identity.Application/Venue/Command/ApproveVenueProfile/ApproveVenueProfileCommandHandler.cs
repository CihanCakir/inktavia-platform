using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public class ApproveVenueProfileCommandHandler : AizenCommandHandler<ApproveVenueProfileCommand, VenueOrganizationRegistrationResponse>
{
    private readonly IUserProfileRepository _profileRepo;
    private readonly UserManager<UserEntity> _userManager;

    public ApproveVenueProfileCommandHandler(IUserProfileRepository profileRepo, UserManager<UserEntity> userManager)
    {
        _profileRepo = profileRepo;
        _userManager = userManager;
    }

        public override async Task<VenueOrganizationRegistrationResponse?> Handle(ApproveVenueProfileCommand request, CancellationToken ct)
        {
           var profile = await _profileRepo.GetProfileByIdAsync(request.ProfileId)
                      ?? throw new AizenBusinessException(((int)AizenErrorCode.ProfileCreateFailed).ToString());

        if (profile.UserId != request.UserId || profile.RoleContext != WorkshopRoleContext.VenueOwner)
            throw new AizenBusinessException(((int)AizenErrorCode.ProfileStatusInvalidForAction).ToString());

        if (profile.ApprovalStatus == ApprovalStatus.Approved)
            throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyApproved).ToString());
        if (profile.ApprovalStatus == ApprovalStatus.Rejected)
            throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyRejected).ToString());

        profile.Approve(request.ReviewedBy);
        _profileRepo.UpdateProfileAsync(profile);

        var user = await _userManager.FindByIdAsync(request.UserId.ToString())
                  ?? throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

        user.SetActiveProfile(profile.Id);
        await _userManager.UpdateAsync(user);

        return new VenueOrganizationRegistrationResponse(
            Success: true,
            Action: "Approve",
            Context: WorkshopRoleContext.VenueOwner,
            UserId: request.UserId,
            ProfileId: request.ProfileId,
            NewStatus: profile.ApprovalStatus,
            ApprovedAt: profile.ApprovedAt,
            RejectedAt: profile.RejectedAt,
            RejectReason: profile.RejectReason,
            Message: "Venue profile approved and set as active.",
            ReviewedBy: profile.ReviewedBy,
            ReviewedAt: profile.ReviewedAt?.ToString("O")
        );
        }
    }
}