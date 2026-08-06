using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryAnalytics;

public sealed class GetCargoDryAnalyticsBffQuery : AizenQuery<GetCargoDryAnalyticsBffResponse>;

public sealed class GetCargoDryAnalyticsBffResponse
{
    public CargoDryAnalyticsBffDto Analytics { get; init; } = default!;
}
