using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Replace the placement set (where the item surfaces). Returns the updated item.</summary>
public sealed class SetContentPlacementsBffCommand : AizenCommand<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
    public SetContentPlacementsRequest Body { get; init; } = default!;
}
