using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.SetContentAudience;

public sealed class SetContentAudienceCommandValidator : AbstractValidator<SetContentAudienceCommand>
{
    public SetContentAudienceCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.Audience).NotNull();
        RuleFor(x => x.Audience.Type).IsInEnum().When(x => x.Audience is not null);
    }
}
