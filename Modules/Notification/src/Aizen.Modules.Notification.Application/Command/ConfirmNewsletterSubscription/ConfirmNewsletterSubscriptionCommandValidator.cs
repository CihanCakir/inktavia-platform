using FluentValidation;

namespace Aizen.Modules.Notification.Application.Command.ConfirmNewsletterSubscription;

public sealed class ConfirmNewsletterSubscriptionCommandValidator : AbstractValidator<ConfirmNewsletterSubscriptionCommand>
{
    public ConfirmNewsletterSubscriptionCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200);
    }
}
