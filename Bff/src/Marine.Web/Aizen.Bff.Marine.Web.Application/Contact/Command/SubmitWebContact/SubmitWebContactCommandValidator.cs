using FluentValidation;

namespace Aizen.Bff.Marine.Web.Application.Contact.Command.SubmitWebContact;

/// <summary>
/// M4 — BFF-side SHAPE validation of the untrusted contact payload (the module re-validates too). Spam is NOT a
/// validation error — a well-formed but spammy message passes shape validation and is handled by the module scorer.
/// The honeypot/captcha fields are intentionally not validated.
/// </summary>
public sealed class SubmitWebContactCommandValidator : AbstractValidator<SubmitWebContactCommand>
{
    public SubmitWebContactCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Message).NotEmpty().MinimumLength(2).MaximumLength(5000);
        RuleFor(x => x.SourcePage).MaximumLength(500);
    }
}
