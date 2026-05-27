using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Model
{
    public sealed class OAuthTempCacheModel
    {
        public required string Nonce { get; init; }
        public required string CodeVerifier { get; init; }
        public string? Redirect { get; init; }
        public required string Provider { get; init; }
    }
}
