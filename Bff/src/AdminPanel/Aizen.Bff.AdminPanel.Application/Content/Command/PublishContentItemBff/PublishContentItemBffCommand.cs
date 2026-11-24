using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Publish now. Returns the updated item.</summary>
public sealed class PublishContentItemBffCommand : AizenCommand<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
}
