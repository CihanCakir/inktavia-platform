using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;

/// <summary>POST /api/v1/mobile/cargodry/kits/validate — validate a scanned serial/batch and (when valid) mint a
/// 5-minute activation token. Behind normal mobile auth so only signed-in owners scan.</summary>
public sealed class ValidateMobileKitCommand : AizenCommand<MobileKitValidationDto>
{
    public ValidateMobileKitCommand(string serialNumber, string batchCode, string? signature)
    {
        SerialNumber = serialNumber;
        BatchCode    = batchCode;
        Signature    = signature;
    }

    public string  SerialNumber { get; }
    public string  BatchCode    { get; }
    public string? Signature    { get; }
}
