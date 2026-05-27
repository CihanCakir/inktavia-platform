using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Dto
{
    public class AccessTokenDto
    {
        public required string AccessToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public required string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}