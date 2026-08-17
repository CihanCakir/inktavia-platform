using Aizen.Core.CQRS.Message;
using Aizen.Modules.Content.Abstraction.Dto;

namespace Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentCategories;

/// <summary>
/// GET /api/v1/web/content/categories — the active editorial category list (flat; hierarchy via ParentSlug).
/// The module DTO is already web-appropriate and passes through.
/// </summary>
public sealed class GetWebContentCategoriesQuery : AizenQuery<List<ContentCategoryDto>>
{
    public string Lang { get; set; } = "tr";
}
