using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.RemoveContentFavorite;

public sealed class RemoveContentFavoriteCommandValidator : AbstractValidator<RemoveContentFavoriteCommand>
{
    public RemoveContentFavoriteCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
    }
}
