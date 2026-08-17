using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Web.Application.Location.Query.GetWebLocationBySlug;

/// <summary>
/// GET /api/v1/web/locations/{slug} — the collapsed single-slug location detail (M3). Resolves the slug via the
/// ReferenceData flat resolver and returns the web location shape (identity + parent chain). Unknown slug → not found.
/// </summary>
public sealed class GetWebLocationBySlugQuery : AizenQuery<WebLocationDetailDto>
{
    public string Slug { get; set; } = default!;
}
