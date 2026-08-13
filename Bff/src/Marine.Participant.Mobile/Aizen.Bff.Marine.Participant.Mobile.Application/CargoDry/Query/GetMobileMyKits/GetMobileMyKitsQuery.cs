using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>GET /api/v1/mobile/cargodry/kits — the caller's kits + roll-up counts. Scoped module-side to
/// <c>OwnerUserId = UserInfo.UserId</c> via the asserted identity; the body carries no user id.</summary>
public sealed class GetMobileMyKitsQuery : AizenQuery<MobileMyKitsDto>
{
}
