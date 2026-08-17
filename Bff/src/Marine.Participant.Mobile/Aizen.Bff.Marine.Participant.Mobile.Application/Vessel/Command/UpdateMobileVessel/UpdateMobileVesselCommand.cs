using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>PUT /api/v1/mobile/vessels/{id} — update a vessel (core + optional spec + optional engine) owned by
/// the authenticated participant, orchestrating the module's update-core → upsert-spec → update-engine handlers.
/// Ownership-gated: a foreign/unknown id yields a clean not-found business error (never another owner's vessel).</summary>
public sealed class UpdateMobileVesselCommand : AizenCommand<MobileVesselDetailDto>
{
    public UpdateMobileVesselCommand(long vesselId, UpdateMobileVesselRequest request)
    {
        VesselId = vesselId;
        Request = request;
    }

    public long VesselId { get; }

    public UpdateMobileVesselRequest Request { get; }
}
