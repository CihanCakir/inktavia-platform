using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryAnalytics;

public sealed class GetCargoDryAnalyticsQuery : AizenQuery<GetCargoDryAnalyticsResponse>;

public sealed class GetCargoDryAnalyticsResponse
{
    public CargoDryAnalyticsDto Analytics { get; init; } = default!;
}
