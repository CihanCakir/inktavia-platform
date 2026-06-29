using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Command.TransferKit;

public sealed class TransferCargoDryKitBffCommand : AizenCommand<TransferCargoDryKitBffCommandResponse>
{
    public long KitId       { get; init; }
    public long NewUserId   { get; init; }
    public long NewVesselId { get; init; }
}

public sealed class TransferCargoDryKitBffCommandResponse
{
    public TransferKitBffResponse Result { get; init; } = default!;
}
