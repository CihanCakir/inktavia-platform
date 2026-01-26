using System.Security.Claims;
using Aizen.Core.Auth.Abstraction;
using Microsoft.AspNetCore.Http;

namespace Aizen.Core.Auth
{
    public class AizenUserService : IAizenUserService
    {
        private readonly IHttpContextAccessor _accessor;

        public AizenUserService(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        public string FindUserNameFromClaimsPrinciple()
        {
            return this.GetUser().Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        public string FindUserIdFromClaimsPrinciple()
        {
            return this.GetUser().Claims?.FirstOrDefault(x => x.Type == "Id")?.Value;
        }

        public ClaimsPrincipal GetUser()
        {
            return _accessor?.HttpContext?.User as ClaimsPrincipal;
        }
    }
}