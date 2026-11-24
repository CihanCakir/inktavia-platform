using FluentValidation;

namespace Aizen.Modules.Notification.Application.Command.UnsubscribeNewsletter;

public sealed class UnsubscribeNewsletterCommandValidator : AbstractValidator<UnsubscribeNewsletterCommand>
{
    public UnsubscribeNewsletterCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200);
    }
}
