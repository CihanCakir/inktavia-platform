using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

public sealed class CargoDryKitActivatedMessage : AizenBaseMessage
{
    public long           KitId         { get; set; }
    public string         KitCode       { get; set; } = default!;
    public string         SerialNumber  { get; set; } = default!;
    public string         ProductName   { get; set; } = default!;
    public long           OwnerUserId   { get; set; }
    public long           VesselId      { get; set; }
    public DateTimeOffset ActivatedAt   { get; set; }
    public DateTimeOffset ExpiresAt     { get; set; }
    public int            ValidityDays  { get; set; }
}
