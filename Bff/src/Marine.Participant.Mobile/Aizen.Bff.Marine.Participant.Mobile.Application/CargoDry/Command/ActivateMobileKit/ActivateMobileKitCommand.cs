using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>POST /api/v1/mobile/cargodry/kits/activate — activate a validated kit onto one of the caller's OWN
/// vessels. Identity is asserted, never taken from the body.</summary>
public sealed class ActivateMobileKitCommand : AizenCommand<MobileKitDto>
{
    public ActivateMobileKitCommand(string activationToken, long vesselId, ActivationMethod method)
    {
        ActivationToken = activationToken;
        VesselId        = vesselId;
        Method          = method;
    }

    public string           ActivationToken { get; }
    public long             VesselId        { get; }
    public ActivationMethod Method          { get; }
}
