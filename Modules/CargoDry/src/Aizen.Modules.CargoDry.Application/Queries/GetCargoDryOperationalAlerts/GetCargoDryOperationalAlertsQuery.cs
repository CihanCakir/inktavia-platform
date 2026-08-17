using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOperationalAlerts;

/// <summary>
/// Returns query-derived operational alerts. No persisted alert table is used.
/// Alerts are computed at query time from kit repository state.
/// Phase 9A — operational alerts foundation.
/// </summary>
public sealed class GetCargoDryOperationalAlertsQuery : AizenQuery<CargoDryOperationalAlertsResponse>
{
    /// <summary>When set, scopes alerts to this provider's kits only.</summary>
    public long? ProviderProfileId { get; init; }
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
