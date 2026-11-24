using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Schedule publish (Draft → Scheduled). Returns the updated item.</summary>
public sealed class ScheduleContentItemBffCommand : AizenCommand<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
    public ScheduleContentItemRequest Body { get; init; } = default!;
}
