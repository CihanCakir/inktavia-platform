using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Commands.CreateContentCategory;

/// <summary>Creates an editorial category (§8). Elevated action (category management, §7).</summary>
public sealed class CreateContentCategoryCommand : AizenCommand<ContentCategoryDto>
{
    public string Slug { get; set; } = default!;
    public Dictionary<string, string> Name { get; set; } = new();
    public string? ParentSlug { get; set; }
    public int Position { get; set; }
    public bool IsActive { get; set; } = true;
}
