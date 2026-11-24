using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Model;

namespace Aizen.Bff.AdminPanel.Application.Content.Command;

/// <summary>Create an editorial category. Returns the created category.</summary>
public sealed class CreateContentCategoryBffCommand : AizenCommand<ContentCategoryBffDto>
{
    public CreateContentCategoryRequest Body { get; init; } = default!;
}
