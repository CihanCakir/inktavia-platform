using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.AddContentComment;

/// <summary>
/// A participant adds a comment on a content item (§8). Author identity is taken from the token in the
/// handler — never from the body.
/// </summary>
public sealed class AddContentCommentCommand : AizenCommand<ContentCommentDto>
{
    public string ContentId { get; set; } = default!;
    public string Body { get; set; } = default!;
    public string? ParentCommentId { get; set; }
}
