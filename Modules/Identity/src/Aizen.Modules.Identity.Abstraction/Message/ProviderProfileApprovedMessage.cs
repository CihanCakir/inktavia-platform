using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Identity.Abstraction.Message;

public sealed class ProviderProfileApprovedMessage : AizenBaseMessage
{
    public long ProfileId { get; set; }
    public long UserId { get; set; }
    public string? Email { get; set; }
    public string ProfileType { get; set; } = default!;
    public DateTime ApprovedAtUtc { get; set; }
}
