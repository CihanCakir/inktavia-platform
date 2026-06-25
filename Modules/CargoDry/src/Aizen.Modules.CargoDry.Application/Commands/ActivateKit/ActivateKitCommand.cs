using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Commands.ActivateKit;

public sealed class ActivateKitCommand : AizenCommand<CargoDryKitDto>
{
    public string           ActivationToken { get; init; } = default!;
    public long             VesselId        { get; init; }
    public long             UserId          { get; init; }
    public ActivationMethod Method          { get; init; } = ActivationMethod.QrScan;
    public ActivationSource Source          { get; init; } = ActivationSource.MobileApp;
    public string?          DeviceInfo      { get; init; }
    public string?          IpAddress       { get; init; }
}
