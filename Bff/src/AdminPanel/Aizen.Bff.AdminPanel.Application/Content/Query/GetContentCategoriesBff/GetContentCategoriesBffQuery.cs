using Aizen.Bff.AdminPanel.Application.Contracts.Content;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Content.Query;

/// <summary>
/// Editorial taxonomy list. The Content module exposes no admin category-list route, so this reads the
/// anonymous public category tree (localized to Lang). Note: returns active categories in the module's
/// public projection — there is no admin-only "include inactive" enumeration.
/// </summary>
public sealed class GetContentCategoriesBffQuery : AizenQuery<List<ContentCategoryBffDto>>
{
    public string Lang { get; init; } = "tr";
}
