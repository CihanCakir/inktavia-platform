using FluentValidation;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.SubscribeWebNewsletter;

/// <summary>P1 — BFF-side SHAPE validation of the untrusted sign-up payload (the module re-validates too).</summary>
public sealed class SubscribeWebNewsletterCommandValidator : AbstractValidator<SubscribeWebNewsletterCommand>
{
    public SubscribeWebNewsletterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Lang).NotEmpty().MaximumLength(10);
        RuleFor(x => x.SourcePage).MaximumLength(500);
    }
}
