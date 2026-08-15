using Aizen.Modules.Content.Abstraction.Enum;

namespace Aizen.Bff.Marine.Web.Application.Content;

/// <summary>
/// The single source of truth for the content surface this BFF serves. The Marine Web BFF always sends
/// <see cref="ContentSurface.MarineOsWeb"/> to the Content module on feed/by-type calls — the web caller can
/// pass lang/paging/type but NEVER the surface. Pinned here and referenced by the feed + by-type handlers.
/// </summary>
public static class WebContentSurface
{
    public const ContentSurface Pinned = ContentSurface.MarineOsWeb;
}
