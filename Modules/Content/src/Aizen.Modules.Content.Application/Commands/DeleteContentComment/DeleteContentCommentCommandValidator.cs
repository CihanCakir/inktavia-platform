using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.DeleteContentComment;

public sealed class DeleteContentCommentCommandValidator : AbstractValidator<DeleteContentCommentCommand>
{
    public DeleteContentCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
    }
}
