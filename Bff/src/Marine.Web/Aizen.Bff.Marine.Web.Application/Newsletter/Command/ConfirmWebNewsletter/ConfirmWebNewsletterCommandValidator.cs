using FluentValidation;

namespace Aizen.Bff.Marine.Web.Application.Newsletter.Command.ConfirmWebNewsletter;

public sealed class ConfirmWebNewsletterCommandValidator : AbstractValidator<ConfirmWebNewsletterCommand>
{
    public ConfirmWebNewsletterCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(200);
    }
}
