using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>Body for replacing the audience of a content item (§8 SetContentAudience).</summary>
public sealed class SetContentAudienceRequest
{
    public ContentAudienceDto Audience { get; set; } = new();
}
