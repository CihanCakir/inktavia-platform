using Aizen.Modules.Content.Abstraction.Enum;
using FluentValidation;

namespace Aizen.Modules.Content.Application.Commands.ModerateContentComment;

public sealed class ModerateContentCommentCommandValidator : AbstractValidator<ModerateContentCommentCommand>
{
    public ModerateContentCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.Status)
            .IsInEnum()
            .NotEqual(ContentCommentStatus.Pending)
            .WithMessage("Pending is not a valid moderation target (use Approved, Rejected or Hidden).");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
