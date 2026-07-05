using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Commands.RenewKit;

public sealed class RenewKitCommand : AizenCommand<CargoDryKitDto>
{
    public long        KitId      { get; init; }
    public int         AddedDays  { get; init; }
    public RenewalType Type       { get; init; }
    public string?     PaymentRef { get; init; }
}
