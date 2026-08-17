using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.AddContentComment;

public sealed class AddContentCommentCommandValidator : AbstractValidator<AddContentCommentCommand>
{
    public AddContentCommentCommandValidator()
    {
        RuleFor(x => x.ContentId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}
