using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Identity.Abstraction.Message;

public sealed class ProviderPasswordRecoveryOtpRequestedMessage : AizenBaseMessage
{
    public long RecipientUserId { get; set; }
    public string Channel { get; set; } = default!;
    public string MaskedTarget { get; set; } = default!;
    public string Otp { get; set; } = default!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int ExpiresInMinutes { get; set; }
}
