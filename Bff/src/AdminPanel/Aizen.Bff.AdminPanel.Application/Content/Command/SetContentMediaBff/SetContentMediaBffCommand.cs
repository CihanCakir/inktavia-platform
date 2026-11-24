using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Replace the media collection (cover + gallery). Returns the updated item.</summary>
public sealed class SetContentMediaBffCommand : AizenCommand<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
    public AttachContentMediaRequest Body { get; init; } = default!;
}
