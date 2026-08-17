using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.AddContentFavorite;

public sealed class AddContentFavoriteCommandValidator : AbstractValidator<AddContentFavoriteCommand>
{
    public AddContentFavoriteCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
    }
}
