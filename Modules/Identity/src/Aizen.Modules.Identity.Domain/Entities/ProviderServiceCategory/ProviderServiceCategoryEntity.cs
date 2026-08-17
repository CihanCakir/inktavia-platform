using Aizen.Core.Domain;

namespace Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;

/// <summary>
/// I2 — a provider's declared service category, normalized out of onboarding (ServiceCapabilities.selectedServiceCategoryIds)
/// into a queryable row. One row per (ProfileId, ServiceCategoryCode). Keyed by ProfileId (the ProviderProfileId used
/// across the platform). Feeds the GetProvidersForArea read-model (region notifications N-C, travel S4).
/// </summary>
// Not sealed: the Identity DbContext uses lazy-loading/change-tracking proxies, which subclass entities at runtime.
public class ProviderServiceCategoryEntity : AizenEntityWithAudit
{
    public long   ProfileId           { get; private set; }
    public long   UserId              { get; private set; }
    /// <summary>Normalized (lower-case) service category code, e.g. "hull-paint", "engine-mechanical".</summary>
    public string ServiceCategoryCode { get; private set; } = default!;

    protected ProviderServiceCategoryEntity() { }

    public static ProviderServiceCategoryEntity Create(long profileId, long userId, string serviceCategoryCode)
        => new()
        {
            ProfileId           = profileId,
            UserId              = userId,
            ServiceCategoryCode = serviceCategoryCode.Trim().ToLowerInvariant(),
        };
}
