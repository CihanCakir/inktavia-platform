using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.AttachContentMedia;

public sealed class AttachContentMediaCommandValidator : AbstractValidator<AttachContentMediaCommand>
{
    public AttachContentMediaCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();

        RuleForEach(x => x.Media).ChildRules(m =>
        {
            m.RuleFor(x => x.FileStorageId).NotEmpty();
            m.RuleFor(x => x.Kind).IsInEnum();
            m.RuleFor(x => x.Position).GreaterThanOrEqualTo(0);
        });
    }
}
