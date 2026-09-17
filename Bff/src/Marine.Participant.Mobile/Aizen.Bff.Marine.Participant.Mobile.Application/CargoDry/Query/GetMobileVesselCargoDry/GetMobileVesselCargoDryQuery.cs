using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>GET /api/v1/mobile/vessels/{id}/cargodry — the CargoDry summary for one of the caller's vessels. Scoped
/// module-side to <c>OwnerUserId = UserInfo.UserId</c> (via GetMyKits) then filtered to the vessel; the id is the only
/// input, the owner is the asserted caller. Never leaks another owner's kits.</summary>
public sealed class GetMobileVesselCargoDryQuery : AizenQuery<MobileVesselCargoDryDto>
{
    public long VesselId { get; }

    public GetMobileVesselCargoDryQuery(long vesselId) => VesselId = vesselId;
}
