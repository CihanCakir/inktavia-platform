using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Identity.Abstraction.Message;

public sealed class ProviderProfileSuspendedMessage : AizenBaseMessage
{
    public long ProfileId { get; set; }
    public long UserId { get; set; }
    public string? Email { get; set; }
    public string Reason { get; set; } = default!;
    public DateTime SuspendedAtUtc { get; set; }
}
