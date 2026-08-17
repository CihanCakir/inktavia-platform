using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.AttachContentMedia;

/// <summary>Replaces the media collection of a content item (§8). Media reference FileStorage assets (B3).</summary>
public sealed class AttachContentMediaCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;
    public List<ContentMediaDto> Media { get; set; } = new();
}
