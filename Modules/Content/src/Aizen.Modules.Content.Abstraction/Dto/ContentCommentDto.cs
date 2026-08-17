using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Abstraction.Dto;

/// <summary>Read model of a participant comment on a content item (§5.2).</summary>
public sealed class ContentCommentDto
{
    public string Id { get; set; } = default!;
    public string ContentId { get; set; } = default!;

    public long AuthorUserId { get; set; }
    public long? AuthorProfileId { get; set; }
    public string? AuthorDisplayName { get; set; }

    public string Body { get; set; } = default!;
    public ContentCommentStatus Status { get; set; }
    public string? ParentCommentId { get; set; }

    // Moderation trail (populated after a moderation action).
    public long? ModeratedByUserId { get; set; }
    public DateTimeOffset? ModeratedAt { get; set; }
    public string? LastModerationReason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
