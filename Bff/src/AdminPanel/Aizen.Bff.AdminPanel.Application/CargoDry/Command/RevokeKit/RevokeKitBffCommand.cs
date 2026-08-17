using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.RevokeKit;

public sealed class RevokeKitBffCommand : AizenCommand<RevokeKitBffCommandResponse>
{
    public long   KitId  { get; init; }
    public string Reason { get; init; } = default!;
}

public sealed class RevokeKitBffCommandResponse
{
    public RevokeKitBffResponse Result { get; init; } = default!;
}
