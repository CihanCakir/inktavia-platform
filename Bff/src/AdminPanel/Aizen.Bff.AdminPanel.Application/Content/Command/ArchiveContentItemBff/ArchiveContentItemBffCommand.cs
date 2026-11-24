using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Archive. Returns the updated item.</summary>
public sealed class ArchiveContentItemBffCommand : AizenCommand<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
}
