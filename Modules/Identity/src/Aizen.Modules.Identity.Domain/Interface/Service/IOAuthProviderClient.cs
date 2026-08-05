using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.Identity.Domain.Interface.Repository
{
    public interface IOAuthProviderClient
    {
        string GetAuthorizeUrl(string provider, string state, string nonce, string codeChallenge, string? uiLocale, string? redirectAfterLogin);
        Task<OAuthTokenExchangeResponse> ExchangeAsync(string provider, string code, string? codeVerifier, CancellationToken ct);
        Task<ValidatedIdTokenDto> ValidateIdTokenAsync(string provider, string idToken, string expectedNonce, CancellationToken ct);

        /// <summary>
        /// Validate a NATIVE (mobile SDK) provider id_token: signature (JWKS), issuer, expiry, and
        /// <c>aud ∈ NativeAudiences</c>. The client nonce is checked only when the token carries one (native
        /// SDK tokens usually have none). Never accepts an unsigned or aud-mismatched token.
        /// </summary>
        Task<ValidatedIdTokenDto> ValidateNativeIdTokenAsync(string provider, string idToken, string? nonce, CancellationToken ct);
    }
}