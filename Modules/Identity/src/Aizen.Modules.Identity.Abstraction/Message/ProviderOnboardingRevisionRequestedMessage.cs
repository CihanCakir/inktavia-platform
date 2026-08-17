using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Identity.Abstraction.Message;

public sealed class ProviderOnboardingRevisionRequestedMessage : AizenBaseMessage
{
    public long ProfileId { get; set; }
    public long UserId { get; set; }
    public string? Email { get; set; }
    public string[] Steps { get; set; } = [];
    public string? Note { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}
