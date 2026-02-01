using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.Identity.Domain.Interface.Repository
{
    public interface IOAuthProviderClient
    {
        string GetAuthorizeUrl(string provider, string state, string nonce, string codeChallenge, string? uiLocale, string? redirectAfterLogin);
        Task<OAuthTokenExchangeResponse> ExchangeAsync(string provider, string code, string? codeVerifier, CancellationToken ct);
        Task<ValidatedIdTokenDto> ValidateIdTokenAsync(string provider, string idToken, string expectedNonce, CancellationToken ct);
    }
}