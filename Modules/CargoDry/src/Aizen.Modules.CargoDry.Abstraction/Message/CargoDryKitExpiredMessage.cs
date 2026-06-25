using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

public sealed class CargoDryKitExpiredMessage : AizenBaseMessage
{
    public long   KitId       { get; set; }
    public string KitCode     { get; set; } = default!;
    public long   OwnerUserId { get; set; }
    public long   VesselId    { get; set; }
}
