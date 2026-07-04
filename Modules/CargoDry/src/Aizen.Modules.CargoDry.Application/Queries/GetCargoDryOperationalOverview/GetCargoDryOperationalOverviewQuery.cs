using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryOperationalOverview;

/// <summary>
/// Returns the rich operational KPI overview for the CargoDry admin dashboard.
/// All values are computed at query time — no financial data included.
/// Phase 9A — dashboard KPI refresh.
/// </summary>
public sealed class GetCargoDryOperationalOverviewQuery : AizenQuery<CargoDryOperationalOverviewDto> { }
