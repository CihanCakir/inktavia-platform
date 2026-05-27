using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Model
{
    public sealed class OAuthOptions
    {
        public required string PublicBaseUrl { get; init; } // örn: https://app.inktavia.com
        public required GoogleOptions Google { get; init; }
        public required AppleOptions Apple { get; init; }
        public sealed class GoogleOptions
        {
            public required string ClientId { get; init; }
            public required string ClientSecret { get; init; }
            public string AuthorizeEndpoint { get; init; } = "https://accounts.google.com/o/oauth2/v2/auth";
            public string TokenEndpoint { get; init; } = "https://oauth2.googleapis.com/token";
            public string Issuer { get; init; } = "https://accounts.google.com";
            public string[] Scopes { get; init; } = new[] { "openid", "email", "profile" };
            public string CallbackPath { get; init; } = "/auth/oauth/callback/google";
        }
        public sealed class AppleOptions
        {
            public required string TeamId { get; init; }
            public required string ClientId { get; init; }  // Service ID
            public required string KeyId { get; init; }
            public required string P8PrivateKeyPem { get; init; }
            public string AuthorizeEndpoint { get; init; } = "https://appleid.apple.com/auth/authorize";
            public string TokenEndpoint { get; init; } = "https://appleid.apple.com/auth/token";
            public string Issuer { get; init; } = "https://appleid.apple.com";
            public string[] Scopes { get; init; } = new[] { "name", "email" };
            public string CallbackPath { get; init; } = "/auth/oauth/callback/apple";
        }
    }
}