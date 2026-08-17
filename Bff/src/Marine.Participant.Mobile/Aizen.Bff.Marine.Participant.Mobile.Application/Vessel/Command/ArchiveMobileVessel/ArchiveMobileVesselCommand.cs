using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>POST /api/v1/mobile/vessels/{id}/archive — archive one of the authenticated participant's vessels
/// (soft; the module has no hard delete). Ownership-gated: a foreign/unknown id yields a clean not-found. Returns
/// the re-read detail (IsArchived = true) so the FE can update its caches; the vessel drops from the active list.</summary>
public sealed class ArchiveMobileVesselCommand : AizenCommand<MobileVesselDetailDto>
{
    public ArchiveMobileVesselCommand(long vesselId, ArchiveMobileVesselRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }

    public long VesselId { get; }

    public ArchiveMobileVesselRequest Request { get; }
}
