using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.ScheduleContentItem;

public sealed class ScheduleContentItemCommandValidator : AbstractValidator<ScheduleContentItemCommand>
{
    public ScheduleContentItemCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.PublishAt).NotEmpty();
        RuleFor(x => x.ExpireAt)
            .GreaterThan(x => x.PublishAt)
            .When(x => x.ExpireAt.HasValue)
            .WithMessage("ExpireAt must be after PublishAt.");
    }
}
