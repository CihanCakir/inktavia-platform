using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Response;
using Aizen.Modules.Identity.Domain.Interface.Repository;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command
{
    public sealed class StartExternalLoginParticipantCommandHandler : AizenCommandHandler<StartExternalLoginParticipantCommand, StartExternalLoginResponse>
    {
        private readonly IOAuthProviderClient _oauth;
        private readonly IAizenDistributedCache _cache;

        public StartExternalLoginParticipantCommandHandler(IOAuthProviderClient oauth, IAizenDistributedCache cache)
        {
            _oauth = oauth; _cache = cache;
        }
        public override async Task<StartExternalLoginResponse?> Handle(StartExternalLoginParticipantCommand request, CancellationToken cancellationToken)
        {
            // 1) state / nonce / pkce
            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

            var codeVerifier = Base64Url(32);
            using var sha = SHA256.Create();
            var challenge = Base64Url(sha.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier)));

            // 2) authorize URL (provider'a göre)
            var authUrl = _oauth.GetAuthorizeUrl(
                provider: request.Provider,
                state: state,
                nonce: nonce,
                codeChallenge: challenge,
                uiLocale: request.UiLocale,
                redirectAfterLogin: request.RedirectAfterLogin
            );

            // 3) cache set (10 dk absolute)
            var temp = new OAuthTempCacheModel
            {
                Nonce = nonce,
                CodeVerifier = codeVerifier,
                Redirect = request.RedirectAfterLogin,
                Provider = request.Provider
            };

            await _cache.SetAsync(
                cacheItem: temp,
                key: "oauth:state:" + state,
                cacheOptions: new AizenCacheOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                },
                token: cancellationToken
            );

            // 4) yanıt
            return new StartExternalLoginResponse(authUrl, state);
        }

        // --- helpers ---
        private static string Base64Url(int lengthBytes)
            => Base64Url(RandomNumberGenerator.GetBytes(lengthBytes));

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
    }
}

