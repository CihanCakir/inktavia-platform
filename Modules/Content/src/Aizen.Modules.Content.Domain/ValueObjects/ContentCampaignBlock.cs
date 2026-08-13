namespace Aizen.Modules.Content.Domain.ValueObjects;

/// <summary>
/// Presentation block for Type=Campaign only (B1): banner/landing CTA copy plus an optional
/// outbound link to the owning commerce system. No pricing or coupon logic lives here.
/// </summary>
public sealed class ContentCampaignBlock
{
    public string? CtaLabel { get; set; }
    public string? CtaUrl { get; set; }

    /// <summary>Optional link out to the campaign's owning system (e.g. Commerce).</summary>
    public ContentExternalRef? ExternalRef { get; set; }
}
