using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Abstraction.Model;

/// <summary>
/// Body for replacing the full placement set of a content item (§8 SetContentPlacements).
/// The supplied list is authoritative — it replaces any existing placements.
/// </summary>
public sealed class SetContentPlacementsRequest
{
    public List<ContentPlacementDto> Placements { get; set; } = new();
}
