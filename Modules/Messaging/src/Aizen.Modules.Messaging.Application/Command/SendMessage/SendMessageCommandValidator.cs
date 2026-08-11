using Aizen.Core.Validation;
using Aizen.Modules.Messaging.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.Messaging.Application.Command.SendMessage;

[DocumentationInfo("Send message command validator", "Validates send message command inputs.")]
public sealed class SendMessageCommandValidator : AizenValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).GreaterThan(0);
        // BE_WC3a — a MediaAttachment (image) carries its payload in the attachment, not the text; Content is legitimately
        // empty there (matches what the SR sync produces). Text/Location still require non-empty Content.
        RuleFor(x => x.Content)
            .NotEmpty().When(x => x.Type != MessageType.MediaAttachment).WithMessage("Message content cannot be empty.")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid message type.");
    }
}
