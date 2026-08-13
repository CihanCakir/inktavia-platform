using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.SetContentPlacements;

public sealed class SetContentPlacementsCommandValidator : AbstractValidator<SetContentPlacementsCommand>
{
    public SetContentPlacementsCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();

        RuleForEach(x => x.Placements).ChildRules(p =>
        {
            p.RuleFor(x => x.Surface).IsInEnum();
            p.RuleFor(x => x.Slot).IsInEnum();
            p.RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
        });
    }
}
