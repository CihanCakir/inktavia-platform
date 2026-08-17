using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Abstraction.Interface.Service;

/// <summary>
/// Calculates an 18-metric performance score for any profile type.
///
/// Hard rules (Phase 19):
/// - Scoring logic must live in Profile.Application — never in AdminPanel BFF.
/// - BFF must remain proxy-only for performance endpoints.
/// - Cold-start: SampleSize &lt; 5 → OverallScore = 50, PriorityTier = Standard,
///   ConfidenceScore ≤ 0.25, MetadataJson contains "coldStart": true.
/// - Automatic punishment, commission changes, or payout holds are NOT performed here.
/// </summary>
public interface IProfilePerformanceEngine
{
    /// <summary>
    /// Calculates the full 5-dimension performance score for the given profile.
    /// Returns a <see cref="ProfileScoreCalculationResult"/> that the caller persists.
    /// Never mutates any entity directly.
    /// </summary>
    Task<ProfileScoreCalculationResult> CalculateAsync(
        long        profileId,
        ProfileType profileType,
        CancellationToken ct = default);
}
