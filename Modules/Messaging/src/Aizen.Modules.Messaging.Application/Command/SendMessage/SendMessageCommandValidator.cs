using Aizen.Core.Validation;
using FluentValidation;

namespace Aizen.Modules.Messaging.Application.Command.SendMessage;

[DocumentationInfo("Send message command validator", "Validates send message command inputs.")]
public sealed class SendMessageCommandValidator : AizenValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).GreaterThan(0);
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid message type.");
    }
}
