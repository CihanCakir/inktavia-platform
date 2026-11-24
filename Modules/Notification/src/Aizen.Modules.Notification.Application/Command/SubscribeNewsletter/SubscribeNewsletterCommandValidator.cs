using FluentValidation;

namespace Aizen.Modules.Notification.Application.Command.SubscribeNewsletter;

/// <summary>
/// P1 — server-side SHAPE validation of the untrusted sign-up payload (auto-run by the CQRS validation pre-processor).
/// A malformed request is a 400; a spammy-but-well-formed request is NOT a validation error (handled by the scorer).
/// </summary>
public sealed class SubscribeNewsletterCommandValidator : AbstractValidator<SubscribeNewsletterCommand>
{
    public SubscribeNewsletterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(10);
        RuleFor(x => x.SourcePage).MaximumLength(500);
    }
}
