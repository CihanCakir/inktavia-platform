using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.Content.Abstraction.Message;

/// <summary>
/// Published on the message bus when a content item leaves the Published state (unpublish/archive),
/// so downstream surfaces/caches can react (§4.9 / B2). Mirror of <see cref="ContentPublishedMessage"/>.
/// Enum values travel as their string names. Keep this contract stable and minimal.
/// </summary>
public sealed class ContentUnpublishedMessage : AizenBaseMessage
{
    public string ContentId { get; set; } = default!;
    public string Slug { get; set; } = default!;

    /// <summary>ContentType as string.</summary>
    public string Type { get; set; } = default!;

    /// <summary>ContentSurface[] as strings.</summary>
    public string[] Surfaces { get; set; } = [];

    /// <summary>ContentAudienceType as string.</summary>
    public string AudienceType { get; set; } = default!;

    public DateTimeOffset UnpublishedAt { get; set; }
}
