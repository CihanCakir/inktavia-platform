using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.UpdateOrganizerProfile
{
    public sealed class UpdateOrganizerProfileCommandHandler
        : AizenCommandHandler<UpdateOrganizerProfileCommand, ProfileUpdateResult>
    {
        private readonly IAizenInfoAccessor _info;
        private readonly UserManager<UserEntity> _userManager;
        private readonly IUserProfileRepository _profileRepo;
        private readonly IUserMessagePermissionRepository _messagePermRepo;

        public UpdateOrganizerProfileCommandHandler(
            IAizenInfoAccessor info,
            UserManager<UserEntity> userManager,
            IUserProfileRepository profileRepo,
            IUserMessagePermissionRepository messagePermRepo)
        {
            _info = info; _userManager = userManager; _profileRepo = profileRepo; _messagePermRepo = messagePermRepo;
        }

        public override async Task<ProfileUpdateResult?> Handle(UpdateOrganizerProfileCommand req, CancellationToken ct)
        {
            var userId = _info.UserInfoAccessor.UserInfo.UserId;
            var user = await _userManager.FindByIdAsync(userId.ToString())
                       ?? throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

            var ctx = WorkshopRoleContext.Organizer;
            var active = await _profileRepo.GetActiveProfileIdAsync(user.Id, ctx);
            if (active is null)
                throw new AizenBusinessException(((int)AizenErrorCode.UserHasNoActiveProfileInThisPanel).ToString());

            if (!string.IsNullOrWhiteSpace(req.OwnerFirstName) || !string.IsNullOrWhiteSpace(req.OwnerLastName))
                active.ChangeName(req.OwnerFirstName ?? active.FirstName, req.OwnerLastName ?? active.LastName);

            if (req.TaxpayerType.HasValue) active.UpdateTaxpayerType(req.TaxpayerType.Value);
            if (!string.IsNullOrWhiteSpace(req.Bio)) active.UpdateBio(req.Bio);
            if (!string.IsNullOrWhiteSpace(req.ProfilePhotoUrl)) active.UpdateProfilePhoto(req.ProfilePhotoUrl);
            if (!string.IsNullOrWhiteSpace(req.NationalityId)) active.NationalityId = req.NationalityId;

            // Provider fixed business location + default per-km rate (partial update: set when supplied).
            if (req.BusinessLatitude.HasValue || req.BusinessLongitude.HasValue || !string.IsNullOrWhiteSpace(req.BusinessAddressLabel))
                active.SetBusinessLocation(req.BusinessLatitude, req.BusinessLongitude, req.BusinessAddressLabel);
            if (req.RatePerKm.HasValue) active.SetRatePerKm(req.RatePerKm);

            _profileRepo.UpdateProfileAsync(active);

            if (req.AllowPush is not null)
                await _messagePermRepo.AddOrUpdateAsync(user.UpsertMessagePermission(MessagePermissionTypes.Notification, null, req.AllowPush), ct);
            if (req.AllowSms is not null)
                await _messagePermRepo.AddOrUpdateAsync(user.UpsertMessagePermission(MessagePermissionTypes.SMS, null, req.AllowSms), ct);
            if (req.AllowEmail is not null)
                await _messagePermRepo.AddOrUpdateAsync(user.UpsertMessagePermission(MessagePermissionTypes.Email, null, req.AllowEmail), ct);

            return new ProfileUpdateResult(true, ctx, user.Id, active.Id, "Organizer profile updated.");
        }
    }
}