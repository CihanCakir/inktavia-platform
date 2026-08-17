using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.SetContentPlacements;

/// <summary>Replaces the full placement set of a content item (§8).</summary>
public sealed class SetContentPlacementsCommand : AizenCommand<ContentItemDto>
{
    public string ContentId { get; set; } = default!;
    public List<ContentPlacementDto> Placements { get; set; } = new();
}
