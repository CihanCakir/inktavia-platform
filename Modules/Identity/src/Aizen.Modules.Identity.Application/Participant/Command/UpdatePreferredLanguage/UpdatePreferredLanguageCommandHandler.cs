using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Aizen.Modules.InktaviaStore.Application.Identity
{
    public sealed class UpdatePreferredLanguageCommandHandler
        : AizenCommandHandler<UpdatePreferredLanguageCommand, UpdatePreferredLanguageResult>
    {
        private readonly IAizenInfoAccessor _info;
        private readonly UserManager<UserEntity> _userManager;

        public UpdatePreferredLanguageCommandHandler(IAizenInfoAccessor info, UserManager<UserEntity> userManager)
        {
            _info = info;
            _userManager = userManager;
        }

        public override async Task<UpdatePreferredLanguageResult?> Handle(
            UpdatePreferredLanguageCommand req, CancellationToken ct)
        {
            var userId = _info.UserInfoAccessor.UserInfo.UserId;
            var user = await _userManager.FindByIdAsync(userId.ToString())
                       ?? throw new AizenBusinessException(((int)AizenErrorCode.UserCreationFailed).ToString());

            // Entity ham girdiyi normalize/doğrular; geçersizse null saklar.
            user.SetPreferredLanguage(req.PreferredLanguage);
            await _userManager.UpdateAsync(user);

            return new UpdatePreferredLanguageResult(true, user.PreferredLanguage);
        }
    }
}
