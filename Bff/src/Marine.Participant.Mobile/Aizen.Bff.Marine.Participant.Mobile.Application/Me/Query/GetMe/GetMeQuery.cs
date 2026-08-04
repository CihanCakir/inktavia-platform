using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Me;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Me;

/// <summary>Lightweight identity echo from the verified Keycloak token (no Identity call).</summary>
public sealed class GetMeQuery : AizenQuery<MeResponse>
{
}
