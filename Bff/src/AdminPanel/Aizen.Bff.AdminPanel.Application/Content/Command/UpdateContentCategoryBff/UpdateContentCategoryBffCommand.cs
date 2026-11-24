using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Update an editorial category (slug immutable). Returns the updated category.</summary>
public sealed class UpdateContentCategoryBffCommand : AizenCommand<ContentCategoryBffDto>
{
    public string Slug { get; init; } = default!;
    public UpdateContentCategoryRequest Body { get; init; } = default!;
}
