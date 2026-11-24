using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Create a content item (module creates it as Draft). Returns the created item.</summary>
public sealed class CreateContentItemBffCommand : AizenCommand<ContentItemBffDto>
{
    public CreateContentItemRequest Body { get; init; } = default!;
}
