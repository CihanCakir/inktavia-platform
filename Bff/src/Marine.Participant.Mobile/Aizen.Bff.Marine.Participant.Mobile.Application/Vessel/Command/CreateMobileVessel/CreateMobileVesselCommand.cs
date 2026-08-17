using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>POST /api/v1/mobile/vessels — create a vessel (core + optional spec + optional engine) for the
/// authenticated participant, orchestrating the module's create → set-spec → add-engine handlers.</summary>
public sealed class CreateMobileVesselCommand : AizenCommand<MobileVesselDetailDto>
{
    public CreateMobileVesselCommand(CreateMobileVesselRequest request)
    {
        Request = request;
    }

    public CreateMobileVesselRequest Request { get; }
}
