using FluentValidation;

namespace Aizen.Modules.Notification.Application.Command.SubmitContactMessage;

/// <summary>
/// M4 — server-side SHAPE validation of the untrusted contact payload (auto-run by the CQRS validation pre-processor).
/// A malformed request is a 400; spam is NOT a validation error (a spammy-but-well-formed message still returns
/// accepted:true and is silently handled by the scorer). The honeypot/captcha fields are intentionally not validated.
/// </summary>
public sealed class SubmitContactMessageCommandValidator : AbstractValidator<SubmitContactMessageCommand>
{
    public SubmitContactMessageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Message).NotEmpty().MinimumLength(2).MaximumLength(5000);
        RuleFor(x => x.SourcePage).MaximumLength(500);
    }
}
