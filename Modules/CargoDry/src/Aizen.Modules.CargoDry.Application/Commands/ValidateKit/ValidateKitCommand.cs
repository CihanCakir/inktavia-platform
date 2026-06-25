using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Commands.ValidateKit;

public sealed class ValidateKitCommand : AizenCommand<CargoDryKitValidationDto>
{
    public string SerialNumber { get; init; } = default!;
    public string BatchCode    { get; init; } = default!;
    public string? Signature   { get; init; }
    public ActivationSource Source { get; init; } = ActivationSource.MobileApp;
}
