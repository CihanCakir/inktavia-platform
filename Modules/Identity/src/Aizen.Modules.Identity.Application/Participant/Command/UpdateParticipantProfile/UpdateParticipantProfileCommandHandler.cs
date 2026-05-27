using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Microsoft.AspNetCore.Identity;


namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public sealed class UpdateParticipantProfileCommandHandler : AizenCommandHandler<UpdateParticipantProfileCommand, ProfileUpdateResult>
    {
        private readonly IAizenInfoAccessor _info;
        private readonly UserManager<UserEntity> _userManager;
        private readonly IUserProfileRepository _profileRepo;
        private readonly IUserMessagePermissionRepository _messagePermRepo;

        public UpdateParticipantProfileCommandHandler(
            IAizenInfoAccessor info,
            UserManager<UserEntity> userManager,
            IUserProfileRepository profileRepo,
            IUserMessagePermissionRepository messagePermRepo)
        {
            _info = info; _userManager = userManager; _profileRepo = profileRepo; _messagePermRepo = messagePermRepo;
        }


        public override async Task<ProfileUpdateResult?> Handle(UpdateParticipantProfileCommand req, CancellationToken ct)
        {
            var userId = _info.UserInfoAccessor.UserInfo.UserId; // projendeki doğru accessor’la değiştir
            var user = await _userManager.FindByIdAsync(userId.ToString())
                       ?? throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

            var ctx = WorkshopRoleContext.Participant;
            var active = await _profileRepo.GetActiveProfileIdAsync(user.Id, ctx);
            if (active is null)
                throw new AizenBusinessException(((int)AizenErrorCode.UserHasNoActiveProfileInThisPanel).ToString());

            // DDD davranışları
            if (!string.IsNullOrWhiteSpace(req.FirstName) || !string.IsNullOrWhiteSpace(req.LastName))
                active.ChangeName(req.FirstName ?? active.FirstName, req.LastName ?? active.LastName);

            if (!string.IsNullOrWhiteSpace(req.Gender)) active.UpdateGender(req.Gender);
            if (req.BirthDate.HasValue) active.UpdateBirthDate(req.BirthDate);
            if (!string.IsNullOrWhiteSpace(req.Bio)) active.UpdateBio(req.Bio);
            if (!string.IsNullOrWhiteSpace(req.ProfilePhotoUrl)) active.UpdateProfilePhoto(req.ProfilePhotoUrl);
            if (!string.IsNullOrWhiteSpace(req.NationalityId)) active.NationalityId = req.NationalityId;

            _profileRepo.UpdateProfileAsync(active);

            // Message permissions (idempotent upsert)
            if (req.AllowPush is not null)
            {
                var p = user.UpsertMessagePermission(MessagePermissionTypes.Notification, null, req.AllowPush);
                await _messagePermRepo.AddOrUpdateAsync(p, ct);
            }
            if (req.AllowSms is not null)
            {
                var p = user.UpsertMessagePermission(MessagePermissionTypes.SMS, null, req.AllowSms);
                await _messagePermRepo.AddOrUpdateAsync(p, ct);
            }
            if (req.AllowEmail is not null)
            {
                var p = user.UpsertMessagePermission(MessagePermissionTypes.Email, null, req.AllowEmail);
                await _messagePermRepo.AddOrUpdateAsync(p, ct);
            }

            return new ProfileUpdateResult(
                Success: true,
                RoleContext: ctx,
                UserId: user.Id,
                ProfileId: active.Id,
                Message: "Consumer profile updated."
            );
        }
    }
}
