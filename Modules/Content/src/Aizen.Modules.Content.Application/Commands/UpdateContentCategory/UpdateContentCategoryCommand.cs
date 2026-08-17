using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.UpdateContentCategory;

/// <summary>Updates an editorial category (§8). Elevated action. Slug is the immutable key.</summary>
public sealed class UpdateContentCategoryCommand : AizenCommand<ContentCategoryDto>
{
    public string Slug { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string? ParentSlug { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; } = true;
}
