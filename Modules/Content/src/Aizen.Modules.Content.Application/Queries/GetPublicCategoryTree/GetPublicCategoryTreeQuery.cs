using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Modules.Content.Application.Queries.GetPublicCategoryTree;

/// <summary>
/// Public editorial category list (§8). Returns active categories ordered by Position; the hierarchy
/// is expressed via ParentSlug (a flat, tree-shaped list — no nested contract type is introduced).
/// Names are returned as the full localized dictionary; Lang is used only for cache-key parity.
/// </summary>
public sealed class GetPublicCategoryTreeQuery : AizenQuery<List<ContentCategoryDto>>
{
    public string Lang { get; set; } = "tr";
}
