using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Update a content item's core editorial fields. Returns the updated item.</summary>
public sealed class UpdateContentItemBffCommand : AizenCommand<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
    public UpdateContentItemRequest Body { get; init; } = default!;
}
