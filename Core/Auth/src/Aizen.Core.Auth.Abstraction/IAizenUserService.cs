using System.Security.Claims;

namespace Aizen.Core.Auth.Abstraction
{
    public interface IAizenUserService
    {
        ClaimsPrincipal GetUser();

        public string FindUserIdFromClaimsPrinciple();
        public string FindUserNameFromClaimsPrinciple();
    }
}