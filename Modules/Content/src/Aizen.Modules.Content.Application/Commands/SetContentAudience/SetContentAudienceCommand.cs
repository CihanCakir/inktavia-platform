using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.SetContentAudience;

/// <summary>Replaces the audience of a content item (§8).</summary>
public sealed class SetContentAudienceCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;
    public ContentAudienceDto Audience { get; set; } = new();
}
