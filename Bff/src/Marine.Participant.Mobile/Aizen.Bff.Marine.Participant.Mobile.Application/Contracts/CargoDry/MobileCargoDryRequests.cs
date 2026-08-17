using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;

/// <summary>Body for POST /api/v1/mobile/cargodry/kits/validate.</summary>
public sealed class MobileValidateKitRequest
{
    public string  SerialNumber { get; set; } = default!;
    public string  BatchCode    { get; set; } = default!;
    public string? Signature    { get; set; }
}

/// <summary>Body for POST /api/v1/mobile/cargodry/kits/activate. No user id — identity is asserted from the token.
/// Method defaults to QrScan (the mobile scan flow); the module accepts the enum by name.</summary>
public sealed class MobileActivateKitRequest
{
    public string           ActivationToken { get; set; } = default!;
    public long             VesselId        { get; set; }
    public ActivationMethod Method          { get; set; } = ActivationMethod.QrScan;
}
