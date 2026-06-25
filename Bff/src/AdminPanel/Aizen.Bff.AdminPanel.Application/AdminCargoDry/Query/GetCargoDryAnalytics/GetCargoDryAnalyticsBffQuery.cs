using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryAnalytics;

public sealed class GetCargoDryAnalyticsBffQuery : AizenQuery<GetCargoDryAnalyticsBffResponse>;

public sealed class GetCargoDryAnalyticsBffResponse
{
    public CargoDryAnalyticsBffDto Analytics { get; init; } = default!;
}
