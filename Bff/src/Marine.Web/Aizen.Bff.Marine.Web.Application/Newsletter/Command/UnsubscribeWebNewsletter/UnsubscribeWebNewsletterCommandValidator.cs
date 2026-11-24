using FluentValidation;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.UnsubscribeWebNewsletter;

public sealed class UnsubscribeWebNewsletterCommandValidator : AbstractValidator<UnsubscribeWebNewsletterCommand>
{
    public UnsubscribeWebNewsletterCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200);
    }
}
