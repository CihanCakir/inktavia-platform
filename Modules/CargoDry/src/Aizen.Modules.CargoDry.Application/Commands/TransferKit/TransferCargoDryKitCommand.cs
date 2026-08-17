using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.CargoDry.Application.Commands.TransferKit;

[DocumentationInfo("Transfer CargoDry kit command",
    "Transfers an Activated kit to a new owner and vessel. " +
    "Only kits in Activated status may be transferred.")]
public sealed class TransferCargoDryKitCommand : AizenCommand<TransferCargoDryKitResponse>
{
    public long KitId        { get; init; }
    public long NewUserId    { get; init; }
    public long NewVesselId  { get; init; }
}
