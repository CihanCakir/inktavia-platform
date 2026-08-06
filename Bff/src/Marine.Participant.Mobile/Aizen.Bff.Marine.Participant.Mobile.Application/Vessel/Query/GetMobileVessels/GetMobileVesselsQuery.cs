using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>GET /api/v1/mobile/vessels — the caller's vessels (scoped by the identity assertion).</summary>
public sealed class GetMobileVesselsQuery : AizenQuery<List<MobileVesselListItemDto>>
{
}
