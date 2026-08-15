using Aizen.Bff.Marine.Web.Application.Common.Options;
using Microsoft.Extensions.Options;

namespace Aizen.Bff.Marine.Web.Application.Common.Seo;

/// <summary>
/// Options-backed <see cref="ISeoIndexabilityPolicy"/> — reads thresholds from <c>MarineWebPublic:Seo</c>. The
/// verdict logic lives here in ONE place; call sites (handlers) only pass a <see cref="SeoEvaluationInput"/> and
/// consume the <see cref="SeoVerdict"/>.
/// FUTURE: a managed-data implementation (ReferenceData SystemParameter) replaces this class without touching any
/// handler — they depend on <see cref="ISeoIndexabilityPolicy"/>, not on Options.
/// </summary>
internal sealed class OptionsSeoIndexabilityPolicy : ISeoIndexabilityPolicy
{
    private readonly IOptionsMonitor<MarineWebPublicOptions> _options;

    public OptionsSeoIndexabilityPolicy(IOptionsMonitor<MarineWebPublicOptions> options) => _options = options;

    public SeoVerdict Evaluate(SeoEvaluationInput input)
    {
        var seo = _options.CurrentValue.Seo;

        if (seo.RequirePublished && !input.IsPublished)
            return new SeoVerdict(false, "not-published");

        if (seo.RequireTitle && string.IsNullOrWhiteSpace(input.Title))
            return new SeoVerdict(false, "missing-title");

        if (seo.RequireDescription && string.IsNullOrWhiteSpace(input.Description))
            return new SeoVerdict(false, "missing-description");

        if (input.BodyLength is { } length)
        {
            if (length < seo.MinBodyLength)
                return new SeoVerdict(false, $"thin-content (body {length} < {seo.MinBodyLength})");

            return new SeoVerdict(true, "indexable");
        }

        // No body at this projection (feed / slug item): the above field checks passed, so it is indexable, but the
        // body threshold was not evaluated — say so, so the reason is honest.
        return new SeoVerdict(true, "indexable (summary-level; body not evaluated)");
    }
}
