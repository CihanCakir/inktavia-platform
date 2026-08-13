using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Content.Abstraction.Message;

/// <summary>
/// Published on the message bus when a content item transitions to Published (§4.9 / B2).
/// Content does NOT dispatch notifications itself — Notification may consume this event and decide.
/// Enum values (Type, Surfaces, AudienceType) travel as their string names to stay forward-compatible.
/// Keep this contract stable and minimal.
/// </summary>
public sealed class ContentPublishedMessage : AizenBaseMessage
{
    public string ContentId { get; set; } = default!;
    public string Slug { get; set; } = default!;

    /// <summary>ContentType as string.</summary>
    public string Type { get; set; } = default!;

    /// <summary>ContentSurface[] as strings.</summary>
    public string[] Surfaces { get; set; } = [];

    /// <summary>ContentAudienceType as string.</summary>
    public string AudienceType { get; set; } = default!;

    public DateTimeOffset PublishedAt { get; set; }
}
