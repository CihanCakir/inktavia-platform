using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RenewKit;

public sealed class RenewKitBffCommand : AizenCommand<RenewKitBffCommandResponse>
{
    public long    KitId      { get; init; }
    public int     AddedDays  { get; init; }
    public string? PaymentRef { get; init; }
}

public sealed class RenewKitBffCommandResponse
{
    public CargoDryKitBffDto Kit { get; init; } = default!;
}
