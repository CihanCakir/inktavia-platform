using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Add or replace a single-language translation. Returns the updated item.</summary>
public sealed class UpsertContentTranslationBffCommand : AizenCommand<ContentItemBffDto>
{
    public string Id { get; init; } = default!;
    public UpsertContentTranslationRequest Body { get; init; } = default!;
}
