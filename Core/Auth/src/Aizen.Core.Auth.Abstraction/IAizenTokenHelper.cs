using System.Security.Claims;

namespace Aizen.Core.Auth.Abstraction
{
    public interface IAizenTokenHelper
    {
        public ClaimsPrincipal GetClaimsPrincipal(string accessToken);
        public string GenerateRefreshToken();
        public string GenerateAccessToken(GenerateAccessTokenModel generateAccessTokenModel);
    }
}