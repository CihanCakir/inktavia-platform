using Aizen.Core.CQRS.Message;
using Aizen.Modules.Messaging.Abstraction.Enum;

namespace Aizen.Modules.Messaging.Application.Command.ModerateMessage;

[DocumentationInfo("Moderate message command", "Carries moderation verdict to apply to a specific message.")]
public sealed class ModerateMessageCommand : AizenCommand<bool>
{
    public long MessageId                  { get; }
    public MessageModerationStatus Status  { get; }
    public string? Reason                  { get; }

    public ModerateMessageCommand(long messageId, MessageModerationStatus status, string? reason)
    {
        MessageId = messageId;
        Status    = status;
        Reason    = reason;
    }
}
