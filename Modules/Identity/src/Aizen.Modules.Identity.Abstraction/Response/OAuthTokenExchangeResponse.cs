using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Response
{
    public sealed record OAuthTokenExchangeResponse(string IdToken, string? Scope);
}