using Aizen.Bff.MarineProvider.Application.Contracts.Auth;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Auth;

/// <summary>
/// Social-login profile provisioning (Flow 2). Called after Keycloak (Google) login by Provider Web.
/// by-subject lookup → provision if missing → write provider_profile_id attribute → ensure pending role.
/// No OAuth exchange here; identity comes from the verified Keycloak token.
/// </summary>
public sealed class EnsureProviderProfileCommand : AizenCommand<EnsureProviderProfileResponse>
{
}
