using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>
/// Body for a participant adding a comment (§8 AddContentComment). The content item is identified by
/// route; the author identity is taken from the token, never the body.
/// </summary>
public sealed class AddContentCommentRequest
{
    public string Body { get; set; } = default!;

    /// <summary>Optional single-level thread parent.</summary>
    public string? ParentCommentId { get; set; }
}

/// <summary>
/// Body for moderating a comment (§8 ModerateContentComment). Status is the target moderation state —
/// valid values are Approved, Rejected or Hidden (Pending is not a moderation target; enforced by the validator).
/// </summary>
public sealed class ModerateContentCommentRequest
{
    public ContentCommentStatus Status { get; set; }

    /// <summary>Optional moderator note / reason.</summary>
    public string? Reason { get; set; }
}
