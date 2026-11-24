using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Delete a content item. Returns the deleted item (module echoes it back).</summary>
public sealed class DeleteContentItemBffCommand : AizenCommand<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
}
