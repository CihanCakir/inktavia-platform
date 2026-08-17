namespace Aizen.Modules.CargoDry.Application.Commands.TransferKit;

public sealed class TransferCargoDryKitResponse
{
    public long   KitId        { get; init; }
    public string KitCode      { get; init; } = default!;
    public string SerialNumber { get; init; } = default!;
    public long   NewUserId    { get; init; }
    public long   NewVesselId  { get; init; }
    public string TransferredAt { get; init; } = default!;
}
