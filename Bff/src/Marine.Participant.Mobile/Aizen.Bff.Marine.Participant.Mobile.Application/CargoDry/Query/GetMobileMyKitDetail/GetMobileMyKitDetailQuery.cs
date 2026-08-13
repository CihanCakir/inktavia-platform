using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>GET /api/v1/mobile/cargodry/kits/{kitId} — owner-scoped kit detail (efficiency / expiry / renewal),
/// served from the caller's OWN kit set. A foreign/unknown id yields a clean not-found.</summary>
public sealed class GetMobileMyKitDetailQuery : AizenQuery<MobileKitDetailDto>
{
    public long KitId { get; }
    public GetMobileMyKitDetailQuery(long kitId) => KitId = kitId;
}
