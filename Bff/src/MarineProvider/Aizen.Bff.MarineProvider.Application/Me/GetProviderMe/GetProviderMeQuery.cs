using Aizen.Bff.MarineProvider.Application.Contracts.Me;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Me.GetProviderMe;

/// <summary>Lightweight identity echo from the verified Keycloak token (no Identity call).</summary>
public sealed class GetProviderMeQuery : AizenQuery<GetProviderMeResponse>
{
}
