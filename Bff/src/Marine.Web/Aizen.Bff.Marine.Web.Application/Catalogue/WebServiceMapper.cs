using System.Text;
using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;

namespace Aizen.Bff.Marine.Web.Application.Catalogue;

/// <summary>
/// ReferenceData <c>LookupItemDto</c> (SERVICE_PROVIDER_CATEGORY group) → web service-catalogue DTO. Strips the
/// internal lookup plumbing and derives a stable URL-safe slug from the code. Field-stripping proven in W5.
/// </summary>
public static class WebServiceMapper
{
    public static WebServiceSummaryDto ToSummary(LookupItemDto i) => new()
    {
        Code = i.Code,
        Slug = ToSlug(i.Code),
        Name = i.Name,
        Description = i.Description,
        IconKey = i.IconKey,
        ColorCode = i.ColorCode,
        SortOrder = i.SortOrder,
    };

    /// <summary>
    /// Deterministic code → URL segment: lower-case, non-alphanumerics collapsed to single dashes, trimmed. This is a
    /// pure transform of an existing value (e.g. <c>ENGINE_REPAIR</c> → <c>engine-repair</c>) — no new data invented.
    /// </summary>
    private static string ToSlug(string code)
    {
        var sb = new StringBuilder(code.Length);
        var lastDash = false;
        foreach (var ch in code.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastDash = false;
            }
            else if (!lastDash)
            {
                sb.Append('-');
                lastDash = true;
            }
        }

        return sb.ToString().Trim('-');
    }
}
