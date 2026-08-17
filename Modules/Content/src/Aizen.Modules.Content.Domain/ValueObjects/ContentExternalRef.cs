namespace Aizen.Modules.Content.Domain.ValueObjects;

/// <summary>
/// Opaque outbound link to another system (e.g. Commerce campaign). Presentation-only — Content
/// links out and never stores discount/coupon/price logic (B1).
/// </summary>
public sealed class ContentExternalRef
{
    /// <summary>Owning system, e.g. "Commerce".</summary>
    public string System { get; set; } = default!;

    /// <summary>Key within that system, e.g. the campaign id.</summary>
    public string Key { get; set; } = default!;
}
