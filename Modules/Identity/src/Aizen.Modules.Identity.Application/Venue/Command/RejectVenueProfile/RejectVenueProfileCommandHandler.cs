using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Message;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public class RejectVenueProfileCommandHandler : AizenCommandHandler<RejectVenueProfileCommand, VenueOrganizationRegistrationResponse>
    {
        private readonly IUserProfileRepository _profileRepo;
        private readonly UserManager<UserEntity> _userManager;
        private readonly IAizenMessagePublisher _publisher;

        public RejectVenueProfileCommandHandler(
            IUserProfileRepository profileRepo,
            UserManager<UserEntity> userManager,
            IAizenMessagePublisher publisher)
        {
            _profileRepo = profileRepo;
            _userManager = userManager;
            _publisher = publisher;
        }

        public override async Task<VenueOrganizationRegistrationResponse?> Handle(RejectVenueProfileCommand request, CancellationToken ct)
        {
            var profile = await _profileRepo.GetProfileByIdAsync(request.ProfileId)
                          ?? throw new AizenBusinessException(((int)AizenErrorCode.ProfileCreateFailed).ToString());

            if (profile.UserId != request.UserId || profile.RoleContext != WorkshopRoleContext.VenueOwner)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileStatusInvalidForAction).ToString());

            var user = await _userManager.FindByIdAsync(request.UserId.ToString())
                      ?? throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

            if (user.ActiveProfileId == profile.Id)
                throw new AizenBusinessException(((int)AizenErrorCode.CannotRejectActiveProfile).ToString());

            if (profile.ApprovalStatus == ApprovalStatus.Approved)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyApproved).ToString());
            if (profile.ApprovalStatus == ApprovalStatus.Rejected)
                throw new AizenBusinessException(((int)AizenErrorCode.ProfileAlreadyRejected).ToString());

            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new AizenBusinessException("Reject reason must be provided.");

            profile.Reject(request.Reason, request.ReasonCategory, request.InternalNote, request.ReviewedBy);
            _profileRepo.UpdateProfileAsync(profile);

            await _publisher.PublishAsync(new ProviderProfileRejectedMessage
            {
                ProfileId = request.ProfileId,
                UserId = request.UserId,
                Email = user.Email,
                ProfileType = "venue",
                Reason = request.Reason,
                ReasonCategory = request.ReasonCategory,
                RejectedAtUtc = profile.RejectedAt ?? DateTime.UtcNow,
            }, ct);

            return new VenueOrganizationRegistrationResponse(
                Success: true,
                Action: "Reject",
                Context: WorkshopRoleContext.VenueOwner,
                UserId: request.UserId,
                ProfileId: request.ProfileId,
                NewStatus: profile.ApprovalStatus,
                ApprovedAt: profile.ApprovedAt,
                RejectedAt: profile.RejectedAt,
                RejectReason: profile.RejectReason,
                Message: "Venue profile rejected.",
                ReviewedBy: profile.ReviewedBy,
                ReviewedAt: profile.ReviewedAt?.ToString("O"),
                RejectionCategory: profile.RejectionCategory
            );
        }
    }

}