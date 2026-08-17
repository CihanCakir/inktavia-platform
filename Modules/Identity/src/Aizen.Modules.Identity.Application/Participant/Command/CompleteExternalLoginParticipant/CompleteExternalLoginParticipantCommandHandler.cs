using System.Security;
using Aizen.Core.Api.Middleware;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Request;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Interface;
using Aizen.Modules.Identity.Domain.Interface.Repository;
using Aizen.Modules.Identity.Repository.Context;
using Aizen.Modules.Payment.Abstraction;
using Microsoft.Extensions.Logging;
using MiniUow;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant
{

    public sealed class CompleteExternalLoginParticipantCommandHandler
        : AizenCommandHandler<CompleteExternalLoginParticipantCommand, UserLoginResponse>
    {
        private const string CacheKeyPrefix = "oauth:state:";

        private readonly IAizenDistributedCache _cache;
        private readonly IOAuthProviderClient _oauth;
        private readonly IRepository<UserEntity> _userRepo;
        private readonly IRepository<UserExternalLoginEntity> _externalRepo;
        private readonly IRepository<UserDeviceEntity> _deviceRepo;
        private readonly IRepository<RoleEntity> _roleRepo;
        private readonly IAuthorizationService _authService; // LoginWithOtpCommandHandler ile aynı servis
        private readonly ILogger<CompleteExternalLoginParticipantCommandHandler> _logger;

        public CompleteExternalLoginParticipantCommandHandler(
            IAizenUnitOfWork<IdentityDbContext> uow,
            IOAuthProviderClient oauth,
            IAizenDistributedCache cache,
            IAuthorizationService authService,
            ILogger<CompleteExternalLoginParticipantCommandHandler> logger)
        {
            _oauth = oauth;
            _cache = cache;
            _userRepo = uow.GetRepository<UserEntity>();
            _externalRepo = uow.GetRepository<UserExternalLoginEntity>();
            _deviceRepo = uow.GetRepository<UserDeviceEntity>();
            _roleRepo = uow.GetRepository<RoleEntity>(); ;
            _authService = authService;
            _logger = logger;
        }

        public override async Task<UserLoginResponse?> Handle(CompleteExternalLoginParticipantCommand cmd, CancellationToken ct)
        {
            // 0) state/nonce/pkce
            var cacheKey = CacheKeyPrefix + cmd.State;
            var temp = await _cache.GetAsync<OAuthTempCacheModel>(cacheKey, ct);
            if (temp is null)
                throw new AizenBusinessException(((int)AizenErrorCode.OAuthInvalidState).ToString());

            // 1) code exchange
            OAuthTokenExchangeResponse exch;
            try
            {
                exch = await _oauth.ExchangeAsync(cmd.Provider, cmd.Code, cmd.CodeVerifier ?? temp.CodeVerifier, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OAuth token exchange failed. Provider={Provider}", cmd.Provider);
                throw new AizenBusinessException(((int)AizenErrorCode.OAuthTokenExchangeFailed).ToString());
            }

            // 2) id_token doğrula
            ValidatedIdTokenDto idt;
            try
            {
                idt = await _oauth.ValidateIdTokenAsync(cmd.Provider, exch.IdToken, temp.Nonce, ct);
            }
            catch (SecurityException se) when (se.Message.Contains("nonce", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(se, "Nonce mismatch. Provider={Provider}", cmd.Provider);
                throw new AizenBusinessException(((int)AizenErrorCode.OAuthNonceMismatch).ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "id_token invalid. Provider={Provider}", cmd.Provider);
                throw new AizenBusinessException(((int)AizenErrorCode.OAuthIdTokenInvalid).ToString());
            }

            var provider = MapProvider(cmd.Provider);      // AuthProvider
            var loginType = MapLoginType(provider);        // LoginType

            // 3) user resolve (external → email → create)
            var ext = await _externalRepo.FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderUserId == idt.Sub && x.IsDeleted == false     );

            UserEntity? user = null;

            if (ext is not null)
            {
                user = await _userRepo.FirstOrDefaultAsync(x => x.Id == ext.UserId && x.IsDeleted == false);
                if (user is null)
                    throw new AizenBusinessException(((int)AizenErrorCode.ExternalUserNotFound).ToString());

                // scope/email güncelle
                ext.Update(idt.Sub, idt.Email, exch.Scope);
                _externalRepo.Update(ext);
            }
            else
            {
                // e-mail ile eşle
                if (!string.IsNullOrWhiteSpace(idt.Email))
                {
                    var emailNorm = idt.Email!.Trim();
                    user = await _userRepo.FirstOrDefaultAsync(x => x.Email == emailNorm && x.IsDeleted == false);
                }

                if (user is null)
                {
                    var email = idt.Email ?? RelayFallback(provider, idt.Sub);
                    user = UserEntity.CreateExternal(email, loginType, idt.EmailVerified);
                    await _userRepo.AddAsync(user);
                }

                var link = UserExternalLoginEntity.Create(user.Id, provider, idt.Sub, idt.Email, exch.Scope);
                await _externalRepo.AddAsync(link);
            }

            // 4) DDD davranışları
            user.ApplyEmailVerificationFromProvider(idt.EmailVerified, idt.Email);

            var participantRole = await _roleRepo.FirstOrDefaultAsync(
                r => r.NormalizedName == RoleNames.Consumer && r.IsDeleted == false);

            if (participantRole is null)
                throw new AizenBusinessException(((int)AizenErrorCode.ParticipantRoleNotFound).ToString());

            var activeProfile = user.EnsureParticipantProfileAndRole(
                participantRole,
                nameFromProvider: idt.Name,
                taxpayerType: TaxpayerType.Individual
            );

            user.LinkOrUpdateExternalLogin(provider, idt.Sub, idt.Email, exch.Scope);
            user.LoginType = loginType;
            user.SetModified();
            _userRepo.Update(user);

            // 5) cihaz kaydı
            if (!string.IsNullOrWhiteSpace(cmd.DeviceId))
            {
                var device = await _deviceRepo.FirstOrDefaultAsync(
                    x => x.DeviceId == cmd.DeviceId && x.UserId == user.Id && x.IsDeleted == false);

                if (device is null)
                {
                    device = new UserDeviceEntity
                    {
                        IsActive = true,
                        DeviceId = cmd.DeviceId!,
                        NotificationToken = cmd.NotificationToken,
                        UserId = user.Id
                    };
                    await _deviceRepo.AddAsync(device);
                }
                else
                {
                    device.IsActive = true;
                    device.NotificationToken = string.IsNullOrWhiteSpace(cmd.NotificationToken)
                        ? device.NotificationToken
                        : cmd.NotificationToken;
                    _deviceRepo.Update(device);
                }
            }

            // 6) token üret
            var tokenResult = await _authService.CreateLoginToken(new UserLoginRequest
            {
                DeviceId = cmd.DeviceId ?? string.Empty,
                Email = user.Email,
                FirstName = activeProfile.FirstName,
                LastName = activeProfile.LastName,
                PhoneNumber = user.PhoneNumber,
                UserId = user.Id,
                NotificationToken = cmd.NotificationToken ?? string.Empty,  
            });

            // 7) temizlik
            await _cache.RemoveAsync<object>(cacheKey, ct);

            return tokenResult;
        }

        private static LoginType MapProvider(string p)
          => p.Equals("apple", StringComparison.OrdinalIgnoreCase) ? LoginType.Apple : LoginType.Google;

        private static LoginType MapLoginType(LoginType provider)
            => provider == LoginType.Apple ? LoginType.Apple : LoginType.Google;

        private static string RelayFallback(LoginType provider, string sub)
            => provider == LoginType.Apple ? $"{sub}@privaterelay.appleid.com" : $"{sub}@users.noreply.oauth";
    }
}
