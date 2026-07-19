using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalCandidates;

/// <summary>
/// Returns kits that are candidates for renewal:
/// - Status = Active
/// - ExpiresAt within the next <see cref="WithinDays"/> days
/// - Do NOT already have an open renewal preparation
/// Phase 11 (July 2026).
/// </summary>
public sealed class GetCargoDryRenewalCandidatesQuery
    : AizenQuery<List<CargoDryRenewalCandidateDto>>
{
    /// <summary>When set, scopes candidates to this provider's kits. Null = global (admin).</summary>
    public long? ProviderProfileId { get; init; }
    /// <summary>Include kits expiring within this many days. Default = 90.</summary>
    public int WithinDays { get; init; } = 90;
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
