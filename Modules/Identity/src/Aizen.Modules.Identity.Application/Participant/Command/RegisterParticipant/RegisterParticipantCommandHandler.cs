using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Enum;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterParticipant;

namespace Aizen.Modules.InktaviaStore.Application.Command.RegisterParticipant
{
    public sealed class RegisterParticipantCommandHandler
        : AizenCommandHandler<RegisterParticipantCommand, RegisterResult>
    {
        private readonly IConsumerRegistrationDomainService _domain;
        private readonly IAuthorizationService _authorization;
        private readonly IUserLoginTokenRepository _tokenRepo;
        private readonly IAizenInfoAccessor _info;

        public RegisterParticipantCommandHandler(
            IConsumerRegistrationDomainService domain,
            IAuthorizationService authorization,
            IUserLoginTokenRepository tokenRepo,
            IAizenInfoAccessor info)
        {
            _domain = domain;
            _authorization = authorization;
            _tokenRepo = tokenRepo;
            _info = info;
        }

        public override async Task<RegisterResult?> Handle(RegisterParticipantCommand request, CancellationToken ct)
        {
            var roleCtx = GetRoleContextFromAppInfo(_info.AppInfoAccessor.AppInfo.Code); // genelde Participant

            // 1) Domain service
            (UserEntity user, UserProfileEntity profile) = await _domain.RegisterOrAttachAsync(new RegisterParticipantDomainModel
            {
                Email = request.Email,
                Phone = request.Phone,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName,
                KvkkAccepted = request.KvkkAccepted,
                DeviceId = request.DeviceId ?? string.Empty,
                DeviceType = request.DeviceType,
                NotificationToken = request.NotificationToken,
                RoleContext = roleCtx,
                RequiredAgreementTypes = new[] { "KVKK", "TERMS_OF_USE" } // gerektiği gibi değiştir
            }, ct);

            // 2) Token üret
            var loginReq = new UserLoginRequest
            {
                UserId = user.Id,
                DeviceId = request.DeviceId ?? string.Empty,
                Email = user.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                PhoneNumber = user.PhoneNumber,
                NotificationToken = request.NotificationToken ?? string.Empty,
                RoleContext = roleCtx,
                ActiveProfileId = profile.Id
            };

            var tokens = await _authorization.CreateLoginToken(loginReq);

            // 3) Token kaydet + eski tokenları revoke et (Aizen akışı)
            await _tokenRepo.AddAsync(new UserLoginTokenEntity
            {
                UserId = user.Id,
                DeviceId = request.DeviceId ?? string.Empty,
                IsRevoked = false,
                AccessToken = tokens.Token.AccessToken ?? string.Empty,
                AccessTokenExpiration = tokens.Token.AccessTokenExpiredDate,
                RefreshToken = tokens.Token.RefreshToken ?? string.Empty,
                RefreshTokenExpiration = tokens.Token.RefreshTokenExpiredDate,
                CreateDate = DateTime.UtcNow
            });

            await _tokenRepo.RevokeOldTokensAsync(user.Id, maxTokenCount: 2);

            // 4) Sonuç
            return new RegisterResult(
                Success: true,
                Status: RegistrationStatus.Active,
                UserId: user.Id,
                ActiveProfileId: profile.Id,
                AccessToken: tokens.Token.AccessToken ?? string.Empty,
                RefreshToken: tokens.Token.RefreshToken ?? string.Empty,
                Message: "Consumer registration completed."
            );
        }

        private static WorkshopRoleContext GetRoleContextFromAppInfo(string platformCode) =>
            platformCode switch
            {
                "ORG" => WorkshopRoleContext.Organizer,
                "VEN" => WorkshopRoleContext.VenueOwner,
                "ADM" => WorkshopRoleContext.Admin,
                _ => WorkshopRoleContext.Participant
            };

    }
}

