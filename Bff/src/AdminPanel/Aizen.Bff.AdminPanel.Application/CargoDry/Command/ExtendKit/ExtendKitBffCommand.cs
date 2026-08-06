using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ExtendKit;

public sealed class ExtendKitBffCommand : AizenCommand<ExtendKitBffCommandResponse>
{
    public long KitId     { get; init; }
    public int  AddedDays { get; init; }
}

public sealed class ExtendKitBffCommandResponse
{
    public CargoDryKitBffDto Kit { get; init; } = default!;
}
