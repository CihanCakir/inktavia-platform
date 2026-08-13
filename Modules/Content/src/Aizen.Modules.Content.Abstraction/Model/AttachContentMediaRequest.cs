using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>
/// Body for setting the media collection of a content item (§8 AttachContentMedia). Media reference
/// FileStorage assets by id/URL (B3); the supplied list is authoritative.
/// </summary>
public sealed class AttachContentMediaRequest
{
    public List<ContentMediaDto> Media { get; set; } = new();
}
