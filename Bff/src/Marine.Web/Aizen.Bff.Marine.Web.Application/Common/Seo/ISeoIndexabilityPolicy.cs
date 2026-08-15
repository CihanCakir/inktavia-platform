namespace Aizen.Bff.Marine.Web.Application.Common.Seo;

/// <summary>
/// Inputs to the indexability verdict. <see cref="BodyLength"/> is null when the projection does not carry a body
/// (feed / slug items) — the policy then evaluates from the fields it has and says so in the reason.
/// </summary>
public sealed record SeoEvaluationInput(
    bool IsPublished,
    string? Title,
    string? Description,
    int? BodyLength);

/// <summary>The backend's indexability verdict for a projection. The frontend enforces it mechanically.</summary>
public sealed record SeoVerdict(bool Indexable, string Reason);

/// <summary>
/// The SEO indexability seam (W3.2). Indexability is the BACKEND's verdict, computed from managed thresholds — never
/// hardcoded at call sites. The current implementation reads thresholds from BFF Options.
/// FUTURE: a managed-data (ReferenceData SystemParameter) implementation swaps in behind this same interface with no
/// change to the handlers that depend on it.
/// </summary>
public interface ISeoIndexabilityPolicy
{
    SeoVerdict Evaluate(SeoEvaluationInput input);
}
